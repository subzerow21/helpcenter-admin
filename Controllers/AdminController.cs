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

        public IActionResult Analytics()
        {
            var productData = new List<ProductMetric>
            {
                new ProductMetric { ProductName = "UltraBoost v2",     UnitsSold = 142, Revenue = 852000m,  Category = "Footwear",    Stock = 25  },
                new ProductMetric { ProductName = "Hydration Pack",    UnitsSold = 98,  Revenue = 147000m,  Category = "Accessories", Stock = 5   },
                new ProductMetric { ProductName = "Swift Shorts",      UnitsSold = 76,  Revenue = 38000m,   Category = "Apparel",     Stock = 40  },
                new ProductMetric { ProductName = "Pro GPS Watch",     UnitsSold = 185, Revenue = 675000m,  Category = "Electronics", Stock = 12  },
                new ProductMetric { ProductName = "Compression Socks", UnitsSold = 210, Revenue = 42000m,   Category = "Apparel",     Stock = 150 }
            };
            var viewModel = new AnalyticsViewModel
            {
                TotalConsumers = 12450,
                TotalSellers = 84,
                TotalRevenue = 7954000m,
                AverageOrderValue = 4850.00,
                ChallengeToSaleConversionRate = 32.8,
                UsersPurchased = 1640,
                TotalOrders = 1240,
                TopMovingProducts = productData.OrderByDescending(p => p.UnitsSold).Take(5).ToList()
            };
            return View(viewModel);
        }

        public IActionResult SellerPerformance()
        {
            var viewModel = new AnalyticsViewModel
            {
                TotalSellers = 124,
                TotalConsumers = 1540,
                TotalRevenue = 2451401.45m,
                TotalOrders = 5840
            };
            return View(viewModel);
        }

        public IActionResult Consumers() => View();
        public IActionResult Sellers() => View();
        public IActionResult Logistics() => View();
        public IActionResult Tasks() => View();
        public IActionResult ChallengeDetails() => View();
        public IActionResult Settings() => View();
        public IActionResult Notifications() => View();

        public IActionResult FinanceRequest()
        {
            var viewModel = new FinanceRequestsViewModel
            {
                PendingPayouts = new List<PayoutRequest>
                {
                    new PayoutRequest { Id="1001", ShopName="TechGear Hub", SellerEmail="owner@techgear.com", Amount=15500.50m, BankName="GCash", SellerNote="Funds Transfer", DateRequested=DateTime.Now.AddDays(-2) },
                    new PayoutRequest { Id="1002", ShopName="Luxe Apparel", SellerEmail="sales@luxe.ph",     Amount=42000.00m, BankName="BDO",   SellerNote="Withdrawal",     DateRequested=DateTime.Now.AddDays(-1) }
                },
                PendingDiscounts = new List<DiscountRequest>
                {
                    new DiscountRequest { Id="D-55", ShopName="TechGear Hub",    ProductName="Wireless Earbuds Pro", OriginalPrice=2500, DiscountPercent=20, StartDate=DateTime.Now,            EndDate=DateTime.Now.AddDays(7)  },
                    new DiscountRequest { Id="D-56", ShopName="Home Essentials", ProductName="Ergonomic Chair",      OriginalPrice=8999, DiscountPercent=50, StartDate=DateTime.Now.AddDays(2), EndDate=DateTime.Now.AddDays(5) }
                }
            };
            return View("FinanceRequest", viewModel);
        }

        // ─────────────────────────────────────────────────────────────────
        public IActionResult HelpCenter()
        {
            // ── FAQs from DB ──────────────────────────────────────────────
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

            // ── Live Queue: ONLY Waiting sessions ─────────────────────────
            var dbSessions = _context.HelpSessions
                .Where(s => s.Status == "Waiting" || s.Status == "No" || s.Status == null)
                .OrderBy(s => s.CreatedAt)
                .ToList()
                .Where(s =>
                {
                    var ut = (s.UserType ?? "").Trim().ToLower();
                    return ut == "consumer" || ut == "seller";
                })
                .ToList();

            var avatarColors = new[] { "#1a1a1a", "#777", "#888", "#999", "#666", "#555", "#333" };

            var sessions = dbSessions.Select((s, i) =>
            {
                var utNorm = (s.UserType ?? "Consumer").Trim();
                var isSeller = utNorm.Equals("Seller", StringComparison.OrdinalIgnoreCase);
                var roleLabel = isSeller ? "Seller" : "Consumer";
                var name = (isSeller ? "Seller" : "Customer") + " #" + s.Id;
                var initials = isSeller ? "SE" : "CU";

                return new QueueSession
                {
                    Id = s.Id,
                    SessionNo = "NH-" + s.Id.ToString("D5"),
                    CustomerName = name,
                    CustomerEmail = string.Empty,
                    Role = roleLabel,
                    Initials = initials,
                    AvatarColor = avatarColors[i % avatarColors.Length],
                    WaitSeconds = (int)(DateTime.Now - s.CreatedAt).TotalSeconds,
                    QueuePosition = i + 1,
                    Status = "waiting",
                    Category = string.IsNullOrWhiteSpace(s.Category) ? "General" : s.Category,
                    AssignedTo = null,
                    Messages = new List<QueueMessage>()
                };
            }).ToList();

            // ── Agents from dbo.Agents ─────────────────────────────────────
            var agentRows = _context.Agents.ToList();

            var agentViewModels = agentRows
                .GroupBy(a => a.AgentName ?? "Unknown")
                .Select(g =>
                {
                    var activeSessions = g.Count(a =>
                        !string.IsNullOrEmpty(a.ChatStatus) &&
                        (a.ChatStatus == "Active" || a.ChatStatus == "active"));

                    var rawStatus = (g.First().AgentStatus ?? "available").ToLower().Trim();
                    var status = rawStatus switch
                    {
                        "available" => "online",
                        "busy" => "busy",
                        "away" => "away",
                        "offline" => "offline",
                        _ => "online"
                    };

                    var name = g.Key ?? "Unknown";
                    var initials = string.Concat(
                        name.Split(' ')
                            .Where(w => w.Length > 0)
                            .Take(2)
                            .Select(w => w[0].ToString().ToUpper()));

                    return new AgentStatusViewModel
                    {
                        Name = name,
                        Initials = initials,
                        Status = status,
                        ActiveSessions = activeSessions,
                        MaxSessions = 3
                    };
                })
                .OrderBy(a => a.Name)
                .ToList();

            // ── Stats ─────────────────────────────────────────────────────
            var resolvedToday = _context.HelpSessions
                .Count(s => s.Status == "Resolved" && s.CreatedAt.Date == DateTime.Today);

            var avgWaitTime = sessions.Any()
                ? TimeSpan.FromSeconds(sessions.Average(s => s.WaitSeconds)).ToString(@"m\:ss")
                : "0:00";

            var agentsOnline = agentViewModels.Count(a => a.Status == "online" || a.Status == "busy");
            var activeSessionsCount = _context.HelpSessions
                .Count(s => s.Status == "Active" || s.Status == "active");

            var viewModel = new HelpCenterV2ViewModel
            {
                Stats = new QueueDashboardStatsViewModel
                {
                    InQueue = sessions.Count,
                    ActiveSessions = activeSessionsCount,
                    AvgWaitTime = avgWaitTime,
                    ResolvedToday = resolvedToday,
                    AbandonRate = 4.2,
                    AgentsOnline = agentsOnline
                },
                Sessions = sessions,
                Agents = agentViewModels,   // ← now from DB, not hardcoded
                Faqs = faqs,
                Categories = categories
            };

            return View(viewModel);
        }

        // ── QUEUE ENDPOINTS ───────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QueueAssign([FromBody] AssignSessionRequest model)
        {
            if (model == null || model.SessionId <= 0 || string.IsNullOrWhiteSpace(model.AgentName))
                return BadRequest(new { success = false, message = "Invalid request." });

            // 1. Mark the HelpSession as Active
            var sessionRows = _context.Database.ExecuteSqlRaw(
                "UPDATE dbo.SupportFAQs SET Status = 'Active', StartTime = GETDATE() WHERE Id = @Id",
                new SqlParameter("@Id", model.SessionId));

            if (sessionRows == 0)
                return Json(new { success = false, message = "Session not found." });

            // 2. Determine next available ChatSlot for this agent (1-3)
            var usedSlots = _context.Agents
                .Where(a => a.AgentName == model.AgentName &&
                            (a.ChatStatus == "Active" || a.ChatStatus == "active"))
                .Select(a => (int)(a.ChatSlot ?? 0))
                .ToList();

            int nextSlot = Enumerable.Range(1, 3).FirstOrDefault(s => !usedSlots.Contains(s));
            if (nextSlot == 0)
                return Json(new { success = false, message = "Agent has no available slots." });

            // 3. Fetch session details
            var session = _context.HelpSessions.Find(model.SessionId);
            var clientName = session != null
                ? ((session.UserType ?? "").Equals("Seller", StringComparison.OrdinalIgnoreCase)
                    ? "Seller #" + session.Id
                    : "Customer #" + session.Id)
                : "Unknown";
            var category = session?.Category ?? "N/A";
            var previewQuestion = session?.Question ?? "N/A";

            // 4. Insert new row into dbo.Agents for this chat slot
            _context.Database.ExecuteSqlRaw(
                @"INSERT INTO dbo.Agents
                    (AgentName, ClientName, Category, PreviewQuestion, ChatSlot, ChatStatus, StartTime, AgentStatus)
                  VALUES
                    (@AgentName, @ClientName, @Category, @PreviewQuestion, @ChatSlot, 'Active', GETDATE(), 'available')",
                new SqlParameter("@AgentName", model.AgentName),
                new SqlParameter("@ClientName", clientName),
                new SqlParameter("@Category", category),
                new SqlParameter("@PreviewQuestion", previewQuestion),
                new SqlParameter("@ChatSlot", Convert.ToByte(nextSlot)));

            return Json(new
            {
                success = true,
                sessionId = model.SessionId,
                agent = model.AgentName,
                slot = nextSlot
            });
        }

        [HttpPost]
        public IActionResult QueueSend([FromBody] QueueSendRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message))
                return BadRequest(new { success = false });
            return Json(new
            {
                success = true,
                message = model.Message,
                isNote = model.IsNote,
                timestamp = DateTime.Now.ToString("h:mm tt")
            });
        }

        [HttpPost]
        public IActionResult QueueEndSession([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });

            _context.Database.ExecuteSqlRaw(
                "UPDATE dbo.SupportFAQs SET Status = 'Resolved', EndTime = GETDATE() WHERE Id = @Id",
                new SqlParameter("@Id", model.SessionId));

            return Json(new { success = true, sessionId = model.SessionId, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueResolve([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });

            _context.Database.ExecuteSqlRaw(
                "UPDATE dbo.SupportFAQs SET Status = 'Resolved', EndTime = GETDATE() WHERE Id = @Id",
                new SqlParameter("@Id", model.SessionId));

            return Json(new { success = true, sessionId = model.SessionId, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueAutoAssign() => Json(new { success = true });

        // ── FAQ ENDPOINTS ─────────────────────────────────────────────────

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

            return Json(new { success = true, id = newId, message = "FAQ added successfully." });
        }

        [HttpPost]
        public IActionResult FaqUpdate([FromBody] UpdateFaqRequest model)
        {
            if (model == null || model.Id <= 0 || string.IsNullOrWhiteSpace(model.Question) || string.IsNullOrWhiteSpace(model.Answer))
                return BadRequest(new { success = false, message = "Invalid request." });

            if (!_context.FAQs.Any(f => f.FaqID == model.Id))
                return NotFound(new { success = false, message = "FAQ not found." });

            var rows = _context.Database.ExecuteSqlRaw(
                @"UPDATE FAQs SET Question=@Question, Answer=@Answer, Category=@Category,
                  UserType=@UserType, LastUpdated=GETDATE() WHERE FaqID=@FaqId",
                new SqlParameter("@Question", model.Question),
                new SqlParameter("@Answer", model.Answer),
                new SqlParameter("@Category", model.Category ?? ""),
                new SqlParameter("@UserType", model.UserType ?? "Consumer"),
                new SqlParameter("@FaqId", model.Id));

            return rows > 0
                ? Json(new { success = true, id = model.Id, message = "FAQ updated." })
                : Json(new { success = false, message = "Failed to update FAQ." });
        }

        [HttpPost]
        public IActionResult FaqDelete([FromBody] DeleteFaqRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { success = false });

            if (!_context.FAQs.Any(f => f.FaqID == model.Id))
                return NotFound(new { success = false, message = "FAQ not found." });

            var rows = _context.Database.ExecuteSqlRaw(
                "UPDATE FAQs SET Status='Inactive', LastUpdated=GETDATE() WHERE FaqID=@FaqId",
                new SqlParameter("@FaqId", model.Id));

            return rows > 0
                ? Json(new { success = true, id = model.Id, message = "FAQ set to inactive." })
                : Json(new { success = false, message = "Failed to deactivate FAQ." });
        }

        [HttpPost]
        public IActionResult FaqCategoryAdd([FromBody] AddCategoryRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false, message = "Category name required." });

            var name = model.Name.Trim();
            var exists = _context.FAQs.Any(f =>
                (f.Status == "active" || f.Status == "Active") &&
                f.Category.ToLower() == name.ToLower());

            return exists
                ? Json(new { success = false, message = "Category already exists." })
                : Json(new { success = true, name = name, message = "Category ready to use." });
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
                name = name,
                deletedCount = rows,
                message = rows > 0 ? $"{rows} FAQ(s) set to inactive." : "Category removed."
            });
        }

        // ── GET available agents for assign dropdown ───────────────────────
        [HttpGet]
        public IActionResult GetAvailableAgents()
        {
            var agentRows = _context.Agents.ToList();

            var result = agentRows
                .GroupBy(a => a.AgentName ?? "Unknown")
                .Select(g =>
                {
                    var activeCount = g.Count(a =>
                        !string.IsNullOrEmpty(a.ChatStatus) &&
                        (a.ChatStatus == "Active" || a.ChatStatus == "active"));

                    var rawStatus = (g.First().AgentStatus ?? "available").ToLower().Trim();

                    return new
                    {
                        name = g.Key,
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

        public IActionResult Logout() => RedirectToAction("Dashboard");
    }
}