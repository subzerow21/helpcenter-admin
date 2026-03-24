using Microsoft.AspNetCore.Mvc;
using NextHorizon.Models;
using NextHorizon.Models.Admin_Models;
using NextHorizon.Services.AdminServices;
using NextHorizon.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NextHorizon.Controllers
{
    public class AdminController : Controller
    {
        private readonly DashboardService _dashboardService = new DashboardService();
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
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

        // ── HELP CENTER ──────────────────────────────────────────────

        public IActionResult HelpCenter()
        {
            // Fetch active FAQs from DB and map to view model
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

            // Get distinct categories from DB
            var categories = _context.FAQs
                .Where(f => f.Status == "active" || f.Status == "Active")
                .Select(f => f.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var viewModel = new HelpCenterV2ViewModel
            {
                Stats = new QueueDashboardStatsViewModel
                {
                    InQueue = 5,
                    ActiveSessions = 8,
                    AvgWaitTime = "2:14",
                    ResolvedToday = 34,
                    AbandonRate = 4.2,
                    AgentsOnline = 3
                },
                Sessions = new List<QueueSession>
                {
                    new QueueSession { Id=1, SessionNo="NH-00421", CustomerName="Maria Reyes",    Role="Consumer", Initials="MR", AvatarColor="#1a1a1a", WaitSeconds=1275, QueuePosition=1, Status="active",  Category="Orders",   AssignedTo="J. Chen", Messages = new List<QueueMessage> { new QueueMessage { Sender="cust", Body="Hi, I placed an order 5 days ago and still haven't received it.", TimeLabel="9:42 AM" }, new QueueMessage { Sender="agt", Body="Hi Maria! Let me check with logistics now.", TimeLabel="9:44 AM" } } },
                    new QueueSession { Id=2, SessionNo="NH-00418", CustomerName="Juan Dela Cruz", Role="Consumer", Initials="JD", AvatarColor="#777",    WaitSeconds=1231, QueuePosition=2, Status="waiting", Category="Returns",  AssignedTo=null,      Messages = new List<QueueMessage>() },
                    new QueueSession { Id=3, SessionNo="NH-00415", CustomerName="Ana Santos",     Role="Seller",   Initials="AS", AvatarColor="#888",    WaitSeconds=1040, QueuePosition=3, Status="waiting", Category="Payments", AssignedTo=null,      Messages = new List<QueueMessage>() },
                    new QueueSession { Id=4, SessionNo="NH-00410", CustomerName="Kevin Tan",      Role="Consumer", Initials="KT", AvatarColor="#999",    WaitSeconds=998,  QueuePosition=4, Status="waiting", Category="Returns",  AssignedTo=null,      Messages = new List<QueueMessage>() },
                    new QueueSession { Id=5, SessionNo="NH-00405", CustomerName="Rodel Garcia",   Role="Seller",   Initials="RG", AvatarColor="#666",    WaitSeconds=1123, QueuePosition=5, Status="waiting", Category="Returns",  AssignedTo=null,      Messages = new List<QueueMessage>() }
                },
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

        // ── QUEUE ENDPOINTS ──────────────────────────────────────────

        [HttpPost]
        public IActionResult QueueAssign([FromBody] AssignSessionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });
            return Json(new { success = true, sessionId = model.SessionId, agent = model.AgentName });
        }

        [HttpPost]
        public IActionResult QueueSend([FromBody] QueueSendRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message))
                return BadRequest(new { success = false });
            return Json(new { success = true, message = model.Message, isNote = model.IsNote, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueEndSession([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });
            return Json(new { success = true, sessionId = model.SessionId, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueResolve([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });
            return Json(new { success = true, sessionId = model.SessionId, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueAutoAssign()
        {
            return Json(new { success = true });
        }

        // ── FAQ ENDPOINTS ────────────────────────────────────────────

        [HttpPost]
        public IActionResult FaqAdd([FromBody] AddFaqRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Question) || string.IsNullOrWhiteSpace(model.Answer))
                return BadRequest(new { success = false, message = "Question and answer are required." });

            var faq = new Faq
            {
                Question = model.Question,
                Answer = model.Answer,
                Category = model.Category,
                UserType = model.UserType,
                Status = "Active",
                user_id = 1,
                DateAdded = DateTime.Now,
                LastUpdated = DateTime.Now
            };

            _context.FAQs.Add(faq);
            _context.SaveChanges();

            return Json(new { success = true, id = faq.FaqID, question = faq.Question, answer = faq.Answer, category = faq.Category, message = "FAQ added successfully." });
        }

        [HttpPost]
        public IActionResult FaqUpdate([FromBody] UpdateFaqRequest model)
        {
            if (model == null || model.Id <= 0 || string.IsNullOrWhiteSpace(model.Question) || string.IsNullOrWhiteSpace(model.Answer))
                return BadRequest(new { success = false, message = "Invalid request." });

            var faq = _context.FAQs.Find(model.Id);
            if (faq == null)
                return NotFound(new { success = false, message = "FAQ not found." });

            faq.Question = model.Question;
            faq.Answer = model.Answer;
            faq.Category = model.Category;
            faq.UserType = model.UserType;
            faq.LastUpdated = DateTime.Now;

            _context.SaveChanges();
            return Json(new { success = true, id = model.Id, message = "FAQ updated." });
        }

        [HttpPost]
        public IActionResult FaqDelete([FromBody] DeleteFaqRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { success = false });

            var faq = _context.FAQs.Find(model.Id);
            if (faq == null)
                return NotFound(new { success = false, message = "FAQ not found." });

            // Soft delete
            faq.Status = "deleted";
            faq.LastUpdated = DateTime.Now;
            _context.SaveChanges();

            return Json(new { success = true, id = model.Id, message = "FAQ deleted." });
        }

        [HttpPost]
        public IActionResult FaqCategoryAdd([FromBody] AddCategoryRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false });
            return Json(new { success = true, name = model.Name });
        }

        [HttpPost]
        public IActionResult FaqCategoryDelete([FromBody] DeleteCategoryRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false });
            return Json(new { success = true, name = model.Name });
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Dashboard");
        }
    }
}