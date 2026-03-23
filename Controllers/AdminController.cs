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
            var viewModel = new QueueDashboardViewModel
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
                        Id=1, SessionNo="NH-00421", CustomerName="Maria Reyes", CustomerEmail="m.reyes@email.com", Role="Consumer",
                        Initials="MR", AvatarColor="#1a1a1a", WaitSeconds=1275, QueuePosition=1, Status="active", Category="orders", AssignedTo="J. Chen",
                        Messages = new List<QueueMessage>
                        {
                            new QueueMessage { Sender="cust", Body="Hi, I placed an order 5 days ago and still haven't received it.", TimeLabel="9:42 AM" },
                            new QueueMessage { Sender="agt", Body="Hi Maria! Let me check with logistics now.", TimeLabel="9:44 AM" }
                        }
                    },
                    new QueueSession { Id=2, SessionNo="NH-00418", CustomerName="Juan Dela Cruz", CustomerEmail="jdc@email.com", Role="Consumer", Initials="JD", AvatarColor="#777", WaitSeconds=1231, QueuePosition=2, Status="waiting", Category="returns" },
                    new QueueSession { Id=3, SessionNo="NH-00415", CustomerName="Ana Santos", CustomerEmail="ana.s@email.com", Role="Seller", Initials="AS", AvatarColor="#888", WaitSeconds=1040, QueuePosition=3, Status="waiting", Category="payments" },
                    new QueueSession { Id=4, SessionNo="NH-00410", CustomerName="Kevin Tan", CustomerEmail="k.tan@email.com", Role="Consumer", Initials="KT", AvatarColor="#999", WaitSeconds=998, QueuePosition=4, Status="waiting", Category="returns" },
                    new QueueSession { Id=5, SessionNo="NH-00405", CustomerName="Rodel Garcia", CustomerEmail="r.garcia@email.com", Role="Seller", Initials="RG", AvatarColor="#666", WaitSeconds=1123, QueuePosition=5, Status="waiting", Category="returns" }
                },
                Agents = new List<AgentStatusViewModel>
                {
                    new AgentStatusViewModel { Name="J. Chen", Initials="JC", Status="busy", ActiveSessions=3, MaxSessions=3 },
                    new AgentStatusViewModel { Name="R. Santos", Initials="RS", Status="busy", ActiveSessions=2, MaxSessions=3 },
                    new AgentStatusViewModel { Name="M. Lim", Initials="ML", Status="online", ActiveSessions=1, MaxSessions=3 }
                }
            };

            return View(viewModel);
        }

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

        public IActionResult Logout()
        {
            return RedirectToAction("Dashboard");
        }
    }
}
