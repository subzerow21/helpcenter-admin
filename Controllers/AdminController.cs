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
                    new DiscountRequest { Id="D-55", ShopName="TechGear Hub",    ProductName="Wireless Earbuds Pro", OriginalPrice=2500, DiscountPercent=20, StartDate=DateTime.Now,            EndDate=DateTime.Now.AddDays(7) },
                    new DiscountRequest { Id="D-56", ShopName="Home Essentials", ProductName="Ergonomic Chair",      OriginalPrice=8999, DiscountPercent=50, StartDate=DateTime.Now.AddDays(2), EndDate=DateTime.Now.AddDays(5) }
                }
            };
            return View("FinanceRequest", viewModel);
        }

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

            // ── ✅ FIXED: SHOW BOTH SELLER + CONSUMER ──────────────────────
            var dbSessions = _context.HelpSessions
                .Where(s =>
                    s.UserType != null &&
                    (
                        s.UserType.ToLower().Contains("consumer") ||
                        s.UserType.ToLower().Contains("seller")
                    ) &&
                    (
                        s.Status == null ||
                        s.Status.ToLower() == "no" ||
                        s.Status.ToLower() == "waiting" ||
                        s.Status.ToLower() == "active"
                    )
                )
                .OrderBy(s => s.CreatedAt)
                .ToList();

            var avatarColors = new[] { "#1a1a1a", "#777", "#888", "#999", "#666", "#555", "#333" };

            var sessions = dbSessions.Select((s, i) =>
            {
                var utNorm = (s.UserType ?? "").Trim().ToLower();

                // ✅ FIX: Proper Seller detection
                var isSeller = utNorm.Contains("seller");

                var roleLabel = isSeller ? "Seller" : "Consumer";
                var prefix = isSeller ? "Seller" : "Customer";
                var name = prefix + " #" + s.Id;
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
                    Status = s.AgentId.HasValue ? "active" : "waiting",
                    Category = string.IsNullOrWhiteSpace(s.Category) ? "General" : s.Category,
                    AssignedTo = s.AgentId.HasValue ? "Agent #" + s.AgentId : null,
                    Messages = new List<QueueMessage>()
                };
            }).ToList();

            // ── Stats ─────────────────────────────────────────────────────
            var resolvedToday = _context.HelpSessions
                .Count(s => s.Status == "Resolved" && s.CreatedAt.Date == DateTime.Today);

            var avgWaitTime = sessions.Any()
                ? TimeSpan.FromSeconds(sessions.Average(s => s.WaitSeconds)).ToString(@"m\:ss")
                : "0:00";

            var viewModel = new HelpCenterV2ViewModel
            {
                Stats = new QueueDashboardStatsViewModel
                {
                    InQueue = sessions.Count,
                    ActiveSessions = sessions.Count(s => s.Status == "active"),
                    AvgWaitTime = avgWaitTime,
                    ResolvedToday = resolvedToday,
                    AbandonRate = 4.2,
                    AgentsOnline = 3
                },
                Sessions = sessions,
                Agents = new List<AgentStatusViewModel>
        {
            new AgentStatusViewModel { Name="J. Chen",     Initials="JC", Status="busy",    ActiveSessions=3, MaxSessions=3 },
            new AgentStatusViewModel { Name="R. Santos",   Initials="RS", Status="busy",    ActiveSessions=2, MaxSessions=3 },
            new AgentStatusViewModel { Name="M. Lim",      Initials="ML", Status="online",  ActiveSessions=1, MaxSessions=3 },
            new AgentStatusViewModel { Name="K. Bautista", Initials="KB", Status="away",    ActiveSessions=0, MaxSessions=3 },
            new AgentStatusViewModel { Name="L. Torres",   Initials="LT", Status="offline", ActiveSessions=0, MaxSessions=3 }
        },
                Faqs = faqs,
                Categories = categories
            };

            return View(viewModel);
        }
        [HttpPost]
        public IActionResult QueueAssign([FromBody] AssignSessionRequest model)
        {
            if (model == null || model.SessionId <= 0) return BadRequest(new { success = false });
            return Json(new { success = true, sessionId = model.SessionId, agent = model.AgentName });
        }

        [HttpPost]
        public IActionResult QueueSend([FromBody] QueueSendRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message)) return BadRequest(new { success = false });
            return Json(new { success = true, message = model.Message, isNote = model.IsNote, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueEndSession([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0) return BadRequest(new { success = false });
            return Json(new { success = true, sessionId = model.SessionId, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueResolve([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0) return BadRequest(new { success = false });
            return Json(new { success = true, sessionId = model.SessionId, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueAutoAssign()
        {
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult FaqAdd([FromBody] AddFaqRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Question) || string.IsNullOrWhiteSpace(model.Answer))
                return BadRequest(new { success = false, message = "Question and answer are required." });

            var sql = @"
                INSERT INTO FAQs (Question, Answer, Category, UserType, Status, user_id, DateAdded, LastUpdated)
                VALUES (@Question, @Answer, @Category, @UserType, 'Active', @UserId, @DateAdded, GETDATE());
                SELECT SCOPE_IDENTITY();";

            _context.Database.ExecuteSqlRaw(sql,
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

            var faqExists = _context.FAQs.Any(f => f.FaqID == model.Id);
            if (!faqExists) return NotFound(new { success = false, message = "FAQ not found." });

            var sql = @"
                UPDATE FAQs
                SET Question = @Question, Answer = @Answer, Category = @Category, UserType = @UserType, LastUpdated = GETDATE()
                WHERE FaqID = @FaqId";

            var rows = _context.Database.ExecuteSqlRaw(sql,
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

            var faqExists = _context.FAQs.Any(f => f.FaqID == model.Id);
            if (!faqExists) return NotFound(new { success = false, message = "FAQ not found." });

            var sql = "UPDATE FAQs SET Status = 'Inactive', LastUpdated = GETDATE() WHERE FaqID = @FaqId";

            var rows = _context.Database.ExecuteSqlRaw(sql,
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

            var sql = "UPDATE FAQs SET Status = 'Inactive', LastUpdated = GETDATE() WHERE Category = @Category AND (Status = 'active' OR Status = 'Active')";

            var rows = _context.Database.ExecuteSqlRaw(sql,
                new SqlParameter("@Category", name));

            return Json(new { success = true, name = name, deletedCount = rows, message = rows > 0 ? $"{rows} FAQ(s) set to inactive." : "Category removed." });
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Dashboard");
        }
    }
}