using Microsoft.AspNetCore.Mvc;
using NextHorizon.Models.Admin_Models;
using NextHorizon.Services.AdminServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NextHorizon.Controllers
{
    public class AdminController : Controller
    {
        private readonly DashboardService _dashboardService = new DashboardService();

        public IActionResult Dashboard()
        {
            var model = _dashboardService.GetHeroStats();
            return View(model);
        }

        public IActionResult Analytics()
        {
            var productData = new List<ProductMetric>
            {
                new ProductMetric { ProductName = "UltraBoost v2", UnitsSold = 142, Revenue = 852000m, Category = "Footwear", Stock = 25 },
                new ProductMetric { ProductName = "Hydration Pack", UnitsSold = 98, Revenue = 147000m, Category = "Accessories", Stock = 5 },
                new ProductMetric { ProductName = "Swift Shorts", UnitsSold = 76, Revenue = 38000m, Category = "Apparel", Stock = 40 },
                new ProductMetric { ProductName = "Pro GPS Watch", UnitsSold = 185, Revenue = 675000m, Category = "Electronics", Stock = 12 },
                new ProductMetric { ProductName = "Compression Socks", UnitsSold = 210, Revenue = 42000m, Category = "Apparel", Stock = 150 }
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
                    new PayoutRequest { Id="1002", ShopName="Luxe Apparel", SellerEmail="sales@luxe.ph", Amount=42000.00m, BankName="BDO", SellerNote="Withdrawal", DateRequested=DateTime.Now.AddDays(-1) }
                },
                PendingDiscounts = new List<DiscountRequest>
                {
                    new DiscountRequest { Id="D-55", ShopName="TechGear Hub", ProductName="Wireless Earbuds Pro", OriginalPrice=2500, DiscountPercent=20, StartDate=DateTime.Now, EndDate=DateTime.Now.AddDays(7) },
                    new DiscountRequest { Id="D-56", ShopName="Home Essentials", ProductName="Ergonomic Chair", OriginalPrice=8999, DiscountPercent=50, StartDate=DateTime.Now.AddDays(2), EndDate=DateTime.Now.AddDays(5) }
                }
            };

            return View("FinanceRequest", viewModel);
        }

        public IActionResult HelpCenter()
        {
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
                    new QueueSession
                    {
                        Id=1, SessionNo="NH-00421", CustomerName="Maria Reyes",
                        CustomerEmail="m.reyes@email.com", Role="Consumer",
                        Initials="MR", AvatarColor="#1a1a1a",
                        WaitSeconds=1275, QueuePosition=1, Status="active",
                        Category="Orders", AssignedTo="J. Chen",
                        Messages = new List<QueueMessage>
                        {
                            new QueueMessage { Sender="cust", Body="Hi, I placed an order 5 days ago and still haven't received it.", TimeLabel="9:42 AM" },
                            new QueueMessage { Sender="agt",  Body="Hi Maria! Let me check with logistics now.",                      TimeLabel="9:44 AM" }
                        }
                    },
                    new QueueSession
                    {
                        Id=2, SessionNo="NH-00418", CustomerName="Juan Dela Cruz",
                        CustomerEmail="jdc@email.com", Role="Consumer",
                        Initials="JD", AvatarColor="#777",
                        WaitSeconds=1231, QueuePosition=2, Status="waiting",
                        Category="Returns", AssignedTo=null,
                        Messages = new List<QueueMessage>()
                    },
                    new QueueSession
                    {
                        Id=3, SessionNo="NH-00415", CustomerName="Ana Santos",
                        CustomerEmail="ana.s@email.com", Role="Seller",
                        Initials="AS", AvatarColor="#888",
                        WaitSeconds=1040, QueuePosition=3, Status="waiting",
                        Category="Payments", AssignedTo=null,
                        Messages = new List<QueueMessage>()
                    },
                    new QueueSession
                    {
                        Id=4, SessionNo="NH-00410", CustomerName="Kevin Tan",
                        CustomerEmail="k.tan@email.com", Role="Consumer",
                        Initials="KT", AvatarColor="#999",
                        WaitSeconds=998, QueuePosition=4, Status="waiting",
                        Category="Returns", AssignedTo=null,
                        Messages = new List<QueueMessage>()
                    },
                    new QueueSession
                    {
                        Id=5, SessionNo="NH-00405", CustomerName="Rodel Garcia",
                        CustomerEmail="r.garcia@email.com", Role="Seller",
                        Initials="RG", AvatarColor="#666",
                        WaitSeconds=1123, QueuePosition=5, Status="waiting",
                        Category="Returns", AssignedTo=null,
                        Messages = new List<QueueMessage>()
                    }
                },
                Agents = new List<AgentStatusViewModel>
                {
                    new AgentStatusViewModel { Name="J. Chen",     Initials="JC", Status="busy",    ActiveSessions=3, MaxSessions=3 },
                    new AgentStatusViewModel { Name="R. Santos",   Initials="RS", Status="busy",    ActiveSessions=2, MaxSessions=3 },
                    new AgentStatusViewModel { Name="M. Lim",      Initials="ML", Status="online",  ActiveSessions=1, MaxSessions=3 },
                    new AgentStatusViewModel { Name="K. Bautista", Initials="KB", Status="away",    ActiveSessions=0, MaxSessions=3 },
                    new AgentStatusViewModel { Name="L. Torres",   Initials="LT", Status="offline", ActiveSessions=0, MaxSessions=3 }
                },
                Faqs = new List<FaqItem>
                {
                    new FaqItem { Id=1, Question="How do I track my order?",                 Answer="Go to My Orders in your account dashboard. Click the order and select Track Shipment. You'll get a real-time update and a tracking link via email.",                        Category="orders"   },
                    new FaqItem { Id=2, Question="What is your return policy?",              Answer="We offer a 30-day hassle-free return policy. Items must be unused and in original packaging. Initiate returns from My Orders → Request Return.",                           Category="returns"  },
                    new FaqItem { Id=3, Question="Do you offer free shipping?",              Answer="Yes! Free standard shipping on all orders over ₱1,500. Express delivery (1–2 days) is available for ₱150. Same-day delivery available in Metro Manila.",                   Category="shipping" },
                    new FaqItem { Id=4, Question="My GCash payment failed but was debited.", Answer="Please provide your transaction reference number. Our finance team will verify within 24 hours and your order will not be cancelled in the meantime.",                      Category="payments" },
                    new FaqItem { Id=5, Question="How do I cancel my order?",                Answer="Orders can be cancelled within 1 hour of placing if not yet processed. Go to My Orders → Cancel Order. After processing, use our return flow.",                            Category="orders"   },
                    new FaqItem { Id=6, Question="How long does delivery take?",             Answer="Standard: 3–5 business days. Express: 1–2 days. Same-day Metro Manila only (orders before 12 NN). You'll receive a tracking link once shipped.",                          Category="shipping" }
                }
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

            if (model.Answer.Length > 500)
                return BadRequest(new { success = false, message = "Answer exceeds 500 characters." });

            // TODO: Save to database
            var newId = new Random().Next(100, 9999);

            return Json(new
            {
                success = true,
                id = newId,
                question = model.Question,
                answer = model.Answer,
                category = model.Category,
                message = "FAQ added successfully."
            });
        }

        [HttpPost]
        public IActionResult FaqUpdate([FromBody] UpdateFaqRequest model)
        {
            if (model == null || model.Id <= 0 || string.IsNullOrWhiteSpace(model.Question) || string.IsNullOrWhiteSpace(model.Answer))
                return BadRequest(new { success = false, message = "Invalid request." });

            // TODO: Update in database

            return Json(new { success = true, id = model.Id, message = "FAQ updated." });
        }

        [HttpPost]
        public IActionResult FaqDelete([FromBody] DeleteFaqRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { success = false });

            // TODO: Delete from database

            return Json(new { success = true, id = model.Id, message = "FAQ deleted." });
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Dashboard");
        }
    }
}