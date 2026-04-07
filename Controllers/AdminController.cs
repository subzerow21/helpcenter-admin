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
                    new DiscountRequest { Id="D-55", ShopName="TechGear Hub",    ProductName="Wireless Earbuds Pro", OriginalPrice=2500, DiscountPercent=20, StartDate=DateTime.Now,            EndDate=DateTime.Now.AddDays(7)  },
                    new DiscountRequest { Id="D-56", ShopName="Home Essentials", ProductName="Ergonomic Chair",      OriginalPrice=8999, DiscountPercent=50, StartDate=DateTime.Now.AddDays(2), EndDate=DateTime.Now.AddDays(5)  }
                }
            };
            return View("FinanceRequest", viewModel);
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELP CENTER — page load
        // ═══════════════════════════════════════════════════════════════════
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

            var agents = BuildAgentViewModels();

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

        // ═══════════════════════════════════════════════════════════════════
        // GET QUEUE SESSIONS
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult GetQueueSessions()
        {
            var dbSessions = _context.HelpSessions
                .Where(s => s.Status == "Waiting")
                .OrderBy(s => s.CreatedAt)
                .ToList();

            var result = dbSessions.Select((s, i) =>
            {
                var isSeller = (s.UserType ?? "").Trim()
                    .Equals("Seller", StringComparison.OrdinalIgnoreCase);
                return new
                {
                    id = s.Id,
                    no = "NH-" + s.Id.ToString("D5"),
                    name = (isSeller ? "Seller" : "Customer") + " #" + s.Id,
                    email = (string)null,
                    role = isSeller ? "Seller" : "Consumer",
                    av = isSeller ? "SE" : "CU",
                    bg = "#1a1a1a",
                    waitSec = (int)(DateTime.Now - s.CreatedAt).TotalSeconds,
                    pos = i + 1,
                    status = "waiting",
                    cat = s.Category ?? "General",
                    agent = "Unassigned"
                };
            }).ToList();

            return Json(result);
        }

        // ── Shared helper — builds agent view models ─────────────────────
        private List<AgentStatusViewModel> BuildAgentViewModels()
        {
            var activeAgentRows = _context.Agents
                .Where(a => (a.ChatStatus == "Active" || a.ChatStatus == "active")
                         && a.ChatStatus != "Resolved"
                         && a.ConversationID != null)
                .ToList();

            var allAgentNames = _context.Agents
                .Select(a => a.AgentName ?? "Unknown")
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            return allAgentNames.Select(name =>
            {
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

                var slots = activeAgentRows
                    .Where(a => a.AgentName == name)
                    .OrderBy(a => a.ChatSlot)
                    .Select(a => new AgentSlot
                    {
                        ConversationId = a.ConversationID ?? 0,
                        ClientName = a.ClientName,
                        Category = a.Category,
                        SlotNumber = (int)(a.ChatSlot ?? 0)
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
        }

        // ═══════════════════════════════════════════════════════════════════
        // GET AGENT STATUS
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult GetAgentStatus()
        {
            var activeAgentRows = _context.Agents
                .Where(a => (a.ChatStatus == "Active" || a.ChatStatus == "active")
                         && a.ChatStatus != "Resolved"
                         && a.ConversationID != null)
                .ToList();

            var allAgentNames = _context.Agents
                .Select(a => a.AgentName ?? "Unknown")
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            var result = allAgentNames.Select(name =>
            {
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

                var slots = activeAgentRows
                    .Where(a => a.AgentName == name)
                    .OrderBy(a => a.ChatSlot)
                    .Select(a => new {
                        convId = a.ConversationID ?? 0,
                        client = a.ClientName,
                        cat = a.Category,
                        slotNum = (int)(a.ChatSlot ?? 0)
                    })
                    .ToList();

                return new
                {
                    name = name,
                    initials = initials,
                    status = mappedStatus,
                    sessions = slots.Count,
                    max = 3,
                    slots = (object)slots
                };
            }).ToList();

            return Json(result);
        }

        // ═══════════════════════════════════════════════════════════════════
        // GET AVAILABLE AGENTS
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult GetAvailableAgents()
        {
            var activeRows = _context.Agents
                .Where(a => (a.ChatStatus == "Active" || a.ChatStatus == "active")
                         && a.ChatStatus != "Resolved"
                         && a.ConversationID != null)
                .ToList();

            var allAgentNames = _context.Agents
                .Select(a => a.AgentName ?? "Unknown")
                .Distinct()
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
                    // TEMP: restriction disabled for testing — original was: activeCount < 3 && rawStatus != "offline"
                    // hasSlot = activeCount < 3 && rawStatus != "offline"
                    hasSlot = rawStatus != "offline"
                };
            })
            .Where(a => a.hasSlot)
            .OrderBy(a => a.activeSessions)
            .ToList();

            return Json(result);
        }

        // ═══════════════════════════════════════════════════════════════════
        // QUEUE ASSIGN
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QueueAssign([FromBody] AssignSessionRequest model)
        {
            if (model == null || model.SessionId <= 0 || string.IsNullOrWhiteSpace(model.AgentName))
                return BadRequest(new { success = false, message = "Invalid request." });

            var agentRow = _context.Agents.FirstOrDefault(a => a.AgentName == model.AgentName);
            if (agentRow == null)
                return Json(new { success = false, message = "Agent not found." });

            var agentUserId = agentRow.UserID;

            var usedSlots = _context.Agents
                .Where(a => a.AgentName == model.AgentName
                         && (a.ChatStatus == "Active" || a.ChatStatus == "active")
                         && a.ChatStatus != "Resolved"
                         && a.ConversationID != null)
                .Select(a => (int)(a.ChatSlot ?? 0))
                .ToList();

            // TEMP: restriction disabled for testing — original limit was 3 slots
            // int nextSlot = Enumerable.Range(1, 3).FirstOrDefault(s => !usedSlots.Contains(s));
            // if (nextSlot == 0)
            //     return Json(new { success = false, message = "Agent has no available slots." });
            int nextSlot = Enumerable.Range(1, 99).FirstOrDefault(s => !usedSlots.Contains(s));

            var session = _context.HelpSessions.Find(model.SessionId);
            if (session == null)
                return Json(new { success = false, message = "Session not found." });

            var isSeller = (session.UserType ?? "").Trim().Equals("Seller", StringComparison.OrdinalIgnoreCase);
            var clientName = (isSeller ? "Seller #" : "Customer #") + session.Id;
            var category = session.Category ?? "General";
            var previewQ = session.Question ?? "";

            _context.Database.ExecuteSqlRaw(
              @"UPDATE dbo.SupportFAQs
                  SET Status = 'Active', StartTime = GETDATE(), AgentId = @AgentId
                  WHERE Id = @Id",
              new SqlParameter("@AgentId", agentUserId.HasValue ? (object)agentUserId.Value : DBNull.Value),
              new SqlParameter("@Id", model.SessionId));

            _context.Database.ExecuteSqlRaw(
                @"INSERT INTO dbo.Agents
                    (ConversationID, AgentName, ClientName, Category, PreviewQuestion,
                     ChatSlot, ChatStatus, AgentStatus, UserID)
                  VALUES
                    (@ConversationID, @AgentName, @ClientName, @Category, @PreviewQuestion,
                     @ChatSlot, 'Active', 'available', @UserID)",
                new SqlParameter("@ConversationID", model.SessionId),
                new SqlParameter("@AgentName", model.AgentName),
                new SqlParameter("@ClientName", clientName),
                new SqlParameter("@Category", category),
                new SqlParameter("@PreviewQuestion", previewQ),
                new SqlParameter("@ChatSlot", Convert.ToByte(nextSlot)),
                new SqlParameter("@UserID", agentUserId.HasValue ? (object)agentUserId.Value : DBNull.Value));

            return Json(new { success = true, sessionId = model.SessionId, agent = model.AgentName, slot = nextSlot });
        }

        // ═══════════════════════════════════════════════════════════════════
        // END SESSION
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult QueueEndSession([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });

            _context.Database.ExecuteSqlRaw(
                "UPDATE dbo.SupportFAQs SET Status='Resolved' WHERE Id=@Id",
                new SqlParameter("@Id", model.SessionId));

            _context.Database.ExecuteSqlRaw(
                @"UPDATE dbo.Agents
                  SET ChatStatus = 'Resolved'
                  WHERE ConversationID = @ConvId
                    AND (ChatStatus = 'Active' OR ChatStatus = 'active')",
                new SqlParameter("@ConvId", model.SessionId));

            return Json(new { success = true, sessionId = model.SessionId });
        }

        // ═══════════════════════════════════════════════════════════════════
        // RESOLVE
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult QueueResolve([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });

            _context.Database.ExecuteSqlRaw(
                "UPDATE dbo.SupportFAQs SET Status='Resolved' WHERE Id=@Id",
                new SqlParameter("@Id", model.SessionId));

            _context.Database.ExecuteSqlRaw(
                @"UPDATE dbo.Agents
                  SET ChatStatus = 'Resolved'
                  WHERE ConversationID = @ConvId
                    AND (ChatStatus = 'Active' OR ChatStatus = 'active')",
                new SqlParameter("@ConvId", model.SessionId));

            return Json(new { success = true, sessionId = model.SessionId });
        }

        [HttpPost]
        public IActionResult QueueSend([FromBody] QueueSendRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message))
                return BadRequest(new { success = false });
            return Json(new { success = true, message = model.Message, isNote = model.IsNote, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueAutoAssign() => Json(new { success = true });

        // ═══════════════════════════════════════════════════════════════════
        // FAQ ENDPOINTS
        // ═══════════════════════════════════════════════════════════════════
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
                "UPDATE FAQs SET Status='Resolved', LastUpdated=GETDATE() WHERE FaqID=@FaqId",
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
                "UPDATE FAQs SET Status='Resolved', LastUpdated=GETDATE() WHERE Category=@Category AND (Status='active' OR Status='Active')",
                new SqlParameter("@Category", name));

            return Json(new
            {
                success = true,
                deletedCount = rows,
                message = rows > 0 ? $"{rows} FAQ(s) set to Resolved." : "Category removed."
            });
        }
    }
}