using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;
using NextHorizon.Models.Admin_Models;
using NextHorizon.Services.AdminServices;
using NextHorizon.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace NextHorizon.Controllers
{
    public class AdminController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
            _dashboardService = new DashboardService();
        }

        public IActionResult Dashboard()
        {
            var model = _dashboardService.GetHeroStats();
            return View(model);
        }

        // ─────────────────────────────────────────────
        // HELP CENTER
        // ─────────────────────────────────────────────
        public IActionResult HelpCenter()
        {
            var faqs = _context.FAQs
                .Where(f => f.Status == "active" || f.Status == "Active")
                .OrderByDescending(f => f.DateAdded)
                .Select(f => new FaqItem
                {
                    Id = f.FaqID,
                    Question = f.Question,
                    Answer = f.Answer,
                    Category = f.Category,
                    UserType = f.UserType
                })
                .ToList();

            var categories = _context.FAQs
                .Where(f => f.Status == "active" || f.Status == "Active")
                .Select(f => f.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Only Waiting sessions show in the queue
            var dbSessions = _context.HelpSessions
                .Where(s => s.Status == "Waiting")
                .OrderBy(s => s.CreatedAt)
                .ToList();

            var sessions = dbSessions.Select((s, i) =>
            {
                var isSeller = (s.UserType ?? "").Trim()
                    .Equals("Seller", StringComparison.OrdinalIgnoreCase);

                return new QueueSession
                {
                    Id = s.Id,
                    SessionNo = "NH-" + s.Id.ToString("D5"),
                    CustomerName = (isSeller ? "Seller" : "Customer") + " #" + s.Id,
                    Role = isSeller ? "Seller" : "Consumer",
                    Initials = isSeller ? "SE" : "CU",
                    AvatarColor = "#1a1a1a",
                    WaitSeconds = (int)(DateTime.Now - s.CreatedAt).TotalSeconds,
                    QueuePosition = i + 1,
                    Status = "waiting",
                    Category = s.Category ?? "General",
                    Messages = new List<QueueMessage>()
                };
            }).ToList();

            // ── Active agent slots ────────────────────────────────────────
            // A slot is active when:
            //   ChatStatus = Active  AND  EndTime IS NULL  AND  ConversationID IS NOT NULL
            // ConversationID stores the SupportFAQs.Id that was assigned to this slot.
            var activeAgentRows = _context.Agents
                .Where(a => (a.ChatStatus == "Active" || a.ChatStatus == "active")
                         && a.EndTime == null
                         && a.ConversationID != null)
                .ToList();

            var allAgentNames = _context.Agents
                .Select(a => a.AgentName ?? "Unknown")
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            var avatarColors = new[] { "#1a1a1a", "#444", "#666", "#888", "#999" };

            var agents = allAgentNames.Select((name, idx) =>
            {
                // Read this agent's current status from their latest row
                var firstRow = _context.Agents.FirstOrDefault(a => a.AgentName == name);
                var rawStatus = (firstRow?.AgentStatus ?? "available").ToLower().Trim();
                var mappedStatus = rawStatus switch
                {
                    "available" => "online",
                    "busy" => "busy",
                    "away" => "away",
                    "offline" => "offline",
                    _ => "online"
                };

                var initials = string.Concat(
                    name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Take(2)
                        .Select(w => char.ToUpper(w[0]).ToString()));

                // Build slots from active (non-resolved) rows only
                // ConversationID = SupportFAQs.Id → shown as Conv# in the panel
                var slots = activeAgentRows
                    .Where(a => a.AgentName == name)
                    .OrderBy(a => a.ChatSlot)
                    .Select(a => new AgentSlot
                    {
                        ConversationId = a.ConversationID ?? 0,  // SupportFAQs.Id
                        ClientName = a.ClientName,
                        Category = a.Category,
                        SlotNumber = a.ChatSlot ?? 0
                    })
                    .ToList();

                return new AgentStatusViewModel
                {
                    Name = name,
                    Initials = initials,
                    Status = mappedStatus,
                    ActiveSessions = slots.Count,
                    MaxSessions = 3,
                    Slots = slots
                };
            }).ToList();

            // ── Stats ─────────────────────────────────────────────────────
            var resolvedToday = _context.HelpSessions
                .Count(s => s.Status == "Resolved" && s.CreatedAt.Date == DateTime.Today);
            var activeCount = _context.HelpSessions.Count(s => s.Status == "Active");
            var agentsOnline = agents.Count(a => a.Status == "online" || a.Status == "busy");
            var avgWaitTime = sessions.Any()
                ? TimeSpan.FromSeconds(sessions.Average(s => s.WaitSeconds)).ToString(@"m\:ss")
                : "0:00";

            var viewModel = new HelpCenterV2ViewModel
            {
                Stats = new QueueDashboardStatsViewModel
                {
                    InQueue = sessions.Count,
                    ActiveSessions = activeCount,
                    AvgWaitTime = avgWaitTime,
                    ResolvedToday = resolvedToday,
                    AgentsOnline = agentsOnline
                },
                Sessions = sessions,
                Agents = agents,
                Faqs = faqs,
                Categories = categories
            };

            return View(viewModel);
        }

        // ─────────────────────────────────────────────
        // QUEUE ASSIGN
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QueueAssign([FromBody] AssignSessionRequest model)
        {
            if (model == null || model.SessionId <= 0 || string.IsNullOrWhiteSpace(model.AgentName))
                return BadRequest(new { success = false, message = "Invalid request." });

            // Get agent's UserID
            var agentRow = _context.Agents
                .FirstOrDefault(a => a.AgentName == model.AgentName);

            if (agentRow == null)
                return Json(new { success = false, message = "Agent not found." });

            var agentUserId = agentRow.UserID;

            // Count only non-resolved active slots for this agent
            // A real slot = Active + EndTime IS NULL + ConversationID IS NOT NULL
            var usedSlots = _context.Agents
                .Where(a => a.AgentName == model.AgentName
                         && (a.ChatStatus == "Active" || a.ChatStatus == "active")
                         && a.EndTime == null
                         && a.ConversationID != null)
                .Select(a => (int)(a.ChatSlot ?? 0))
                .ToList();

            int nextSlot = Enumerable.Range(1, 3)
                .FirstOrDefault(s => !usedSlots.Contains(s));

            if (nextSlot == 0)
                return Json(new { success = false, message = "Agent has no available slots." });

            // Fetch session from SupportFAQs
            var session = _context.HelpSessions.Find(model.SessionId);
            if (session == null)
                return Json(new { success = false, message = "Session not found." });

            var isSeller = (session.UserType ?? "").Trim()
                .Equals("Seller", StringComparison.OrdinalIgnoreCase);
            var clientName = (isSeller ? "Seller #" : "Customer #") + session.Id;
            var category = session.Category ?? "General";
            var previewQ = session.Question ?? "";

            // Mark SupportFAQs row as Active
            _context.Database.ExecuteSqlRaw(
                @"UPDATE dbo.SupportFAQs
                  SET Status = 'Active', StartTime = GETDATE(), AgentId = @AgentId
                  WHERE Id = @Id",
                new SqlParameter("@AgentId", agentUserId.HasValue ? (object)agentUserId.Value : DBNull.Value),
                new SqlParameter("@Id", model.SessionId));

            // Insert agent slot row
            // ConversationID = SupportFAQs.Id — this is the link used for resolve/display
            _context.Database.ExecuteSqlRaw(
                @"INSERT INTO dbo.Agents
                    (ConversationID, AgentName, ClientName, Category, PreviewQuestion,
                     ChatSlot, ChatStatus, StartTime, AgentStatus, UserID)
                  VALUES
                    (@ConversationID, @AgentName, @ClientName, @Category, @PreviewQuestion,
                     @ChatSlot, 'Active', GETDATE(), 'available', @UserID)",
                new SqlParameter("@ConversationID", model.SessionId),   // ← SupportFAQs.Id
                new SqlParameter("@AgentName", model.AgentName),
                new SqlParameter("@ClientName", clientName),
                new SqlParameter("@Category", category),
                new SqlParameter("@PreviewQuestion", previewQ),
                new SqlParameter("@ChatSlot", Convert.ToByte(nextSlot)),
                new SqlParameter("@UserID", agentUserId.HasValue ? (object)agentUserId.Value : DBNull.Value));

            return Json(new
            {
                success = true,
                sessionId = model.SessionId,
                agent = model.AgentName,
                slot = nextSlot
            });
        }

        // ─────────────────────────────────────────────
        // END SESSION
        // ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult QueueEndSession([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });

            // Mark SupportFAQs row as Resolved
            _context.Database.ExecuteSqlRaw(
                "UPDATE dbo.SupportFAQs SET Status='Resolved', EndTime=GETDATE() WHERE Id=@Id",
                new SqlParameter("@Id", model.SessionId));

            // Free the agent slot — match by ConversationID = SessionId
            _context.Database.ExecuteSqlRaw(
                @"UPDATE dbo.Agents
                  SET ChatStatus='Resolved', EndTime=GETDATE()
                  WHERE ConversationID = @ConvId
                    AND ChatStatus = 'Active'
                    AND EndTime IS NULL",
                new SqlParameter("@ConvId", model.SessionId));

            return Json(new { success = true, sessionId = model.SessionId });
        }

        // ─────────────────────────────────────────────
        // RESOLVE
        // ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult QueueResolve([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });

            // Mark SupportFAQs row as Resolved
            _context.Database.ExecuteSqlRaw(
                "UPDATE dbo.SupportFAQs SET Status='Resolved', EndTime=GETDATE() WHERE Id=@Id",
                new SqlParameter("@Id", model.SessionId));

            // Free the agent slot — match by ConversationID = SessionId
            _context.Database.ExecuteSqlRaw(
                @"UPDATE dbo.Agents
                  SET ChatStatus='Resolved', EndTime=GETDATE()
                  WHERE ConversationID = @ConvId
                    AND ChatStatus = 'Active'
                    AND EndTime IS NULL",
                new SqlParameter("@ConvId", model.SessionId));

            return Json(new { success = true, sessionId = model.SessionId });
        }

        // ─────────────────────────────────────────────
        // AVAILABLE AGENTS FOR DROPDOWN
        // ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult GetAvailableAgents()
        {
            var allAgentNames = _context.Agents
                .Select(a => a.AgentName ?? "Unknown")
                .Distinct()
                .ToList();

            // Only count real active slots
            var activeRows = _context.Agents
                .Where(a => (a.ChatStatus == "Active" || a.ChatStatus == "active")
                         && a.EndTime == null
                         && a.ConversationID != null)   // ← consistent with assign/resolve
                .ToList();

            var result = allAgentNames.Select(name =>
            {
                var agentRow = _context.Agents.FirstOrDefault(a => a.AgentName == name);
                var rawStatus = (agentRow?.AgentStatus ?? "available").ToLower().Trim();
                var activeCount = activeRows.Count(a => a.AgentName == name);

                return new
                {
                    name = name,
                    activeSessions = activeCount,
                    maxSessions = 3,
                    status = rawStatus,
                    hasSlot = activeCount < 3 && rawStatus != "offline"
                };
            })
            .Where(a => a.hasSlot)
            .OrderBy(a => a.activeSessions)
            .ToList();

            return Json(result);
        }

        // ─────────────────────────────────────────────
        // FAQ ENDPOINTS
        // ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult FaqAdd([FromBody] AddFaqRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Question) || string.IsNullOrWhiteSpace(model.Answer))
                return BadRequest(new { success = false, message = "Question and answer are required." });

            _context.Database.ExecuteSqlRaw(
                @"INSERT INTO FAQs (Question, Answer, Category, UserType, Status, user_id, DateAdded, LastUpdated)
                  VALUES (@Question, @Answer, @Category, @UserType, 'Active', @UserId, @DateAdded, GETDATE())",
                new SqlParameter("@Question", model.Question),
                new SqlParameter("@Answer", model.Answer),
                new SqlParameter("@Category", model.Category ?? ""),
                new SqlParameter("@UserType", model.UserType ?? "Consumer"),
                new SqlParameter("@UserId", 1),
                new SqlParameter("@DateAdded", DateTime.Now));

            var newId = _context.FAQs
                .Where(f => f.Question == model.Question && f.Status == "Active")
                .OrderByDescending(f => f.DateAdded)
                .Select(f => f.FaqID)
                .FirstOrDefault();

            return Json(new { success = true, id = newId });
        }

        [HttpPost]
        public IActionResult FaqUpdate([FromBody] UpdateFaqRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { success = false });

            var rows = _context.Database.ExecuteSqlRaw(
                @"UPDATE FAQs SET Question=@Question, Answer=@Answer, Category=@Category,
                  UserType=@UserType, LastUpdated=GETDATE() WHERE FaqID=@FaqId",
                new SqlParameter("@Question", model.Question),
                new SqlParameter("@Answer", model.Answer),
                new SqlParameter("@Category", model.Category ?? ""),
                new SqlParameter("@UserType", model.UserType ?? "Consumer"),
                new SqlParameter("@FaqId", model.Id));

            return rows > 0
                ? Json(new { success = true })
                : Json(new { success = false, message = "Update failed." });
        }

        [HttpPost]
        public IActionResult FaqDelete([FromBody] DeleteFaqRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { success = false });

            var rows = _context.Database.ExecuteSqlRaw(
                "UPDATE FAQs SET Status='Inactive', LastUpdated=GETDATE() WHERE FaqID=@FaqId",
                new SqlParameter("@FaqId", model.Id));

            return rows > 0
                ? Json(new { success = true })
                : Json(new { success = false, message = "Failed to deactivate." });
        }

        [HttpPost]
        public IActionResult FaqCategoryAdd([FromBody] AddCategoryRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false });

            var name = model.Name.Trim();
            var exists = _context.FAQs.Any(f =>
                (f.Status == "active" || f.Status == "Active") &&
                f.Category.ToLower() == name.ToLower());

            return exists
                ? Json(new { success = false, message = "Category already exists." })
                : Json(new { success = true, name = name });
        }

        [HttpPost]
        public IActionResult FaqCategoryDelete([FromBody] DeleteCategoryRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false });

            var name = model.Name.Trim();
            var rows = _context.Database.ExecuteSqlRaw(
                "UPDATE FAQs SET Status='Inactive', LastUpdated=GETDATE() WHERE Category=@Category AND (Status='active' OR Status='Active')",
                new SqlParameter("@Category", name));

            return Json(new
            {
                success = true,
                deletedCount = rows,
                message = rows > 0 ? $"{rows} FAQ(s) set to inactive." : "Category removed."
            });
        }

        // ─────────────────────────────────────────────
        // OTHER ACTIONS
        // ─────────────────────────────────────────────
        public IActionResult Analytics() => View();
        public IActionResult SellerPerformance() => View();
        public IActionResult Consumers() => View();
        public IActionResult Sellers() => View();
        public IActionResult Logistics() => View();
        public IActionResult Tasks() => View();
        public IActionResult ChallengeDetails() => View();
        public IActionResult Settings() => View();
        public IActionResult Notifications() => View();
        public IActionResult Logout() => RedirectToAction("Dashboard");

        public IActionResult FinanceRequest()
        {
            var viewModel = new FinanceRequestsViewModel
            {
                PendingPayouts = new List<PayoutRequest>
                {
                    new PayoutRequest { Id="1001", ShopName="TechGear Hub",  SellerEmail="owner@techgear.com", Amount=15500.50m, BankName="GCash", SellerNote="Funds Transfer", DateRequested=DateTime.Now.AddDays(-2) },
                    new PayoutRequest { Id="1002", ShopName="Luxe Apparel",  SellerEmail="sales@luxe.ph",     Amount=42000.00m, BankName="BDO",   SellerNote="Withdrawal",     DateRequested=DateTime.Now.AddDays(-1) }
                },
                PendingDiscounts = new List<DiscountRequest>
                {
                    new DiscountRequest { Id="D-55", ShopName="TechGear Hub",    ProductName="Wireless Earbuds Pro", OriginalPrice=2500, DiscountPercent=20, StartDate=DateTime.Now,            EndDate=DateTime.Now.AddDays(7) },
                    new DiscountRequest { Id="D-56", ShopName="Home Essentials", ProductName="Ergonomic Chair",      OriginalPrice=8999, DiscountPercent=50, StartDate=DateTime.Now.AddDays(2), EndDate=DateTime.Now.AddDays(5) }
                }
            };
            return View("FinanceRequest", viewModel);
        }
    }
}