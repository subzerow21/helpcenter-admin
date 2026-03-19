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

        // URL: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            var model = _dashboardService.GetHeroStats();
            return View(model);
        }

        // URL: /Admin/Analytics
        public IActionResult Analytics()
        {
            var productData = new List<ProductMetric>
            {
                new ProductMetric { ProductName = "UltraBoost v2",     UnitsSold = 142, Revenue = 852000m,  Category = "Footwear",     Stock = 25  },
                new ProductMetric { ProductName = "Hydration Pack",    UnitsSold = 98,  Revenue = 147000m,  Category = "Accessories",  Stock = 5   },
                new ProductMetric { ProductName = "Swift Shorts",      UnitsSold = 76,  Revenue = 38000m,   Category = "Apparel",      Stock = 40  },
                new ProductMetric { ProductName = "Pro GPS Watch",     UnitsSold = 185, Revenue = 675000m,  Category = "Electronics",  Stock = 12  },
                new ProductMetric { ProductName = "Compression Socks", UnitsSold = 210, Revenue = 42000m,   Category = "Apparel",      Stock = 150 }
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

                PerformanceTrends = new List<AnalyticsChartData>
                {
                    new AnalyticsChartData { DateLabel = "Mar 01", ChallengeParticipants = 120, TotalRevenue = 110000m },
                    new AnalyticsChartData { DateLabel = "Mar 02", ChallengeParticipants = 150, TotalRevenue = 135000m },
                    new AnalyticsChartData { DateLabel = "Mar 03", ChallengeParticipants = 95,  TotalRevenue = 95000m  },
                    new AnalyticsChartData { DateLabel = "Mar 04", ChallengeParticipants = 210, TotalRevenue = 160000m },
                    new AnalyticsChartData { DateLabel = "Mar 05", ChallengeParticipants = 300, TotalRevenue = 210000m },
                    new AnalyticsChartData { DateLabel = "Mar 06", ChallengeParticipants = 280, TotalRevenue = 195000m },
                    new AnalyticsChartData { DateLabel = "Mar 07", ChallengeParticipants = 350, TotalRevenue = 240000m }
                },

                TopSellers = new List<SellerMetric>
                {
                    new SellerMetric { Rank=1, SellerName="Elite Strides Ph",   ShopName="Elite Sports Store",     OrdersFulfilled=452, RevenueGenerated=1254000.50m },
                    new SellerMetric { Rank=2, SellerName="Mountain Peak Gear", ShopName="Peak Adventure Shop",    OrdersFulfilled=310, RevenueGenerated=890600.00m  },
                    new SellerMetric { Rank=3, SellerName="Urban Runner Co.",   ShopName="Urban Runner Flagship",  OrdersFulfilled=285, RevenueGenerated=412000.75m  },
                    new SellerMetric { Rank=4, SellerName="Velocity Sports",    ShopName="Velocity Metro",         OrdersFulfilled=198, RevenueGenerated=356000.00m  },
                    new SellerMetric { Rank=5, SellerName="HydroFlow Official", ShopName="HydroFlow PH",           OrdersFulfilled=156, RevenueGenerated=98000.25m   }
                }.OrderByDescending(s => s.RevenueGenerated).ToList(),

                TopMovingProducts = productData.OrderByDescending(p => p.UnitsSold).Take(5).ToList(),

                PeakEngagementData = new List<HourlyEngagementMetric>
                {
                    new HourlyEngagementMetric { Hour=0,  ActivitySyncCount=20,  PurchaseCount=5   },
                    new HourlyEngagementMetric { Hour=6,  ActivitySyncCount=420, PurchaseCount=15  },
                    new HourlyEngagementMetric { Hour=9,  ActivitySyncCount=150, PurchaseCount=40  },
                    new HourlyEngagementMetric { Hour=12, ActivitySyncCount=110, PurchaseCount=85  },
                    new HourlyEngagementMetric { Hour=17, ActivitySyncCount=580, PurchaseCount=60  },
                    new HourlyEngagementMetric { Hour=21, ActivitySyncCount=310, PurchaseCount=190 }
                }.OrderBy(h => h.Hour).ToList()
            };

            return View(viewModel);
        }

        // URL: /Admin/SellerPerformance
        public IActionResult SellerPerformance()
        {
            var mockSellers = new List<SellerMetric>
            {
                new SellerMetric { Rank=1, SellerName="Nike Official",      ShopName="Nike PH Store",          OrdersFulfilled=1450, RevenueGenerated=850400.00m,  GrowthPercentage=12.5  },
                new SellerMetric { Rank=2, SellerName="Adidas Philippines", ShopName="Adidas PH Store",        OrdersFulfilled=1200, RevenueGenerated=720300.50m,  GrowthPercentage=8.2   },
                new SellerMetric { Rank=3, SellerName="Under Armour",       ShopName="UA Performance Center",  OrdersFulfilled=890,  RevenueGenerated=450000.75m,  GrowthPercentage=15.0  },
                new SellerMetric { Rank=4, SellerName="Titan 22",           ShopName="Titan Basketball",       OrdersFulfilled=760,  RevenueGenerated=310200.20m,  GrowthPercentage=5.4   },
                new SellerMetric { Rank=5, SellerName="Puma Metro",         ShopName="Puma Metro Hub",         OrdersFulfilled=540,  RevenueGenerated=120500.00m,  GrowthPercentage=-2.1  }
            };

            var mockProducts = new List<ProductMetric>
            {
                new ProductMetric { ProductName="Vaporfly Next%",         Category="Running",    SalesCount=320, Revenue=1500000m },
                new ProductMetric { ProductName="Yoga Mat Pro",            Category="Fitness",    SalesCount=280, Revenue=56000m   },
                new ProductMetric { ProductName="Dumbbell Set 10kg",       Category="Fitness",    SalesCount=150, Revenue=75000m   },
                new ProductMetric { ProductName="Stainless Water Bottle",  Category="Accessories",SalesCount=410, Revenue=92000m   },
                new ProductMetric { ProductName="Performance Socks",       Category="Apparel",    SalesCount=520, Revenue=25000m   }
            }.OrderByDescending(p => p.Revenue).ToList();

            var viewModel = new AnalyticsViewModel
            {
                TotalSellers = 124,
                TotalConsumers = 1540,
                TotalRevenue = 2451401.45m,
                TotalOrders = 5840,
                TopSellers = mockSellers,
                TopProducts = mockProducts
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
                    new PayoutRequest { Id="1001", ShopName="TechGear Hub",  SellerEmail="owner@techgear.com", Amount=15500.50m, BankName="GCash", SellerNote="Funds Transfer", DateRequested=DateTime.Now.AddDays(-2) },
                    new PayoutRequest { Id="1002", ShopName="Luxe Apparel",  SellerEmail="sales@luxe.ph",      Amount=42000.00m, BankName="BDO",   SellerNote="Withdrawal",     DateRequested=DateTime.Now.AddDays(-1) }
                },
                PendingDiscounts = new List<DiscountRequest>
                {
                    new DiscountRequest { Id="D-55", ShopName="TechGear Hub",     ProductName="Wireless Earbuds Pro", OriginalPrice=2500, DiscountPercent=20, StartDate=DateTime.Now,           EndDate=DateTime.Now.AddDays(7) },
                    new DiscountRequest { Id="D-56", ShopName="Home Essentials",  ProductName="Ergonomic Chair",      OriginalPrice=8999, DiscountPercent=50, StartDate=DateTime.Now.AddDays(2), EndDate=DateTime.Now.AddDays(5) }
                }
            };
            return View("FinanceRequest", viewModel);
        }

        // ══════════════════════════════════════════════
        //  HELP CENTER  (URL: /Admin/HelpCenter)
        // ══════════════════════════════════════════════

        public IActionResult HelpCenter()
        {
            var viewModel = new HelpCenterViewModel
            {
                Stats = new HelpCenterStatsViewModel
                {
                    TotalTickets = 1284,
                    OpenTickets = 47,
                    PendingTickets = 12,
                    UrgentTickets = 6,
                    ResolvedToday = 23,
                    AvgResponse = "1.4h"
                },
                Tickets = new List<SupportTicket>
                {
                    new SupportTicket
                    {
                        Id=1, TicketNo="NH-00421", Subject="Order not received",
                        CustomerName="Maria Reyes", CustomerEmail="m.reyes@email.com",
                        Initials="MR", AvatarColor="#1a1a1a",
                        Status="urgent", Category="orders", DateLabel="Mar 17", Role="Consumer",
                        Messages = new List<TicketMessage>
                        {
                            new TicketMessage { Sender="user",  Initials="MR", AvatarColor="#1a1a1a", Body="Hi, I placed order NH-00421 five days ago and still haven't received it. The tracking hasn't updated since March 13. Please help!", TimeLabel="Mar 17, 9:42 AM" },
                            new TicketMessage { Sender="admin", Initials="JC", AvatarColor="#e8e8e8", Body="Hi Maria! I'm so sorry to hear that. I've escalated this to our logistics team and will follow up within 2 hours with a full update.", TimeLabel="Mar 17, 10:05 AM" },
                            new TicketMessage { Sender="user",  Initials="MR", AvatarColor="#1a1a1a", Body="Thank you! Please check if it was delivered to the wrong address — the driver sometimes mislabels packages.", TimeLabel="Mar 17, 10:11 AM" }
                        }
                    },
                    new SupportTicket
                    {
                        Id=2, TicketNo="NH-00418", Subject="Return request — wrong item shipped",
                        CustomerName="Juan Dela Cruz", CustomerEmail="jdc@email.com",
                        Initials="JD", AvatarColor="#444",
                        Status="open", Category="returns", DateLabel="Mar 16", Role="Consumer",
                        Messages = new List<TicketMessage>
                        {
                            new TicketMessage { Sender="user",  Initials="JD", AvatarColor="#444",    Body="I received the wrong item in my order NH-00418. I ordered a black headphone but received a white one.", TimeLabel="Mar 16, 2:30 PM" },
                            new TicketMessage { Sender="admin", Initials="JC", AvatarColor="#e8e8e8", Body="Hello Juan! I sincerely apologize for the mix-up. I'll arrange a free return pickup and ship the correct item immediately.", TimeLabel="Mar 16, 3:00 PM" }
                        }
                    },
                    new SupportTicket
                    {
                        Id=3, TicketNo="NH-00415", Subject="GCash payment debited, order still unpaid",
                        CustomerName="Ana Santos", CustomerEmail="ana.s@email.com",
                        Initials="AS", AvatarColor="#666",
                        Status="pending", Category="payments", DateLabel="Mar 15", Role="Seller",
                        Messages = new List<TicketMessage>
                        {
                            new TicketMessage { Sender="user",  Initials="AS", AvatarColor="#666",    Body="My GCash was debited but my order shows as unpaid and may be cancelled. Transaction ref: GC-884721.", TimeLabel="Mar 15, 11:00 AM" },
                            new TicketMessage { Sender="admin", Initials="JC", AvatarColor="#e8e8e8", Body="Hi Ana! I've forwarded this to our finance team. Please expect a resolution within 24 hours. Your order will NOT be cancelled.", TimeLabel="Mar 15, 11:45 AM" },
                            new TicketMessage { Sender="user",  Initials="AS", AvatarColor="#666",    Body="Thank you for the quick response, really appreciate it!", TimeLabel="Mar 15, 12:02 PM" }
                        }
                    },
                    new SupportTicket
                    {
                        Id=4, TicketNo="NH-00410", Subject="Refund not processed after return",
                        CustomerName="Kevin Tan", CustomerEmail="k.tan@email.com",
                        Initials="KT", AvatarColor="#888",
                        Status="open", Category="returns", DateLabel="Mar 14", Role="Consumer",
                        Messages = new List<TicketMessage>
                        {
                            new TicketMessage { Sender="user", Initials="KT", AvatarColor="#888", Body="I returned my item 8 days ago and still no refund. My return tracking shows it was delivered to your warehouse.", TimeLabel="Mar 14, 9:00 AM" },
                            new TicketMessage { Sender="admin", Initials="JC", AvatarColor="#e8e8e8", Body="Hi Kevin, thanks for flagging this. I have escalated your refund verification to our finance team and will update you within today.", TimeLabel="Mar 14, 9:25 AM" }
                        }
                    },
                    new SupportTicket
                    {
                        Id=5, TicketNo="NH-00408", Subject="Discount code not working at checkout",
                        CustomerName="Liza Mendoza", CustomerEmail="liza.m@email.com",
                        Initials="LM", AvatarColor="#333",
                        Status="closed", Category="orders", DateLabel="Mar 13", Role="Consumer",
                        Messages = new List<TicketMessage>
                        {
                            new TicketMessage { Sender="user",  Initials="LM", AvatarColor="#333",    Body="My promo code SAVE20 says invalid at checkout even though the email says it's valid until end of March.", TimeLabel="Mar 13, 3:15 PM" },
                            new TicketMessage { Sender="admin", Initials="JC", AvatarColor="#e8e8e8", Body="Hi Liza! The code unfortunately expired on Mar 12. As a courtesy, I've applied a 20% manual discount directly to your cart.", TimeLabel="Mar 13, 3:45 PM" },
                            new TicketMessage { Sender="user",  Initials="LM", AvatarColor="#333",    Body="That worked! Thank you so much, you're amazing!", TimeLabel="Mar 13, 4:00 PM" }
                        }
                    },
                    new SupportTicket
                    {
                        Id=6, TicketNo="NH-00405", Subject="Product arrived with cracked screen",
                        CustomerName="Rodel Garcia", CustomerEmail="r.garcia@email.com",
                        Initials="RG", AvatarColor="#555",
                        Status="urgent", Category="returns", DateLabel="Mar 13", Role="Seller",
                        Messages = new List<TicketMessage>
                        {
                            new TicketMessage { Sender="user", Initials="RG", AvatarColor="#555", Body="My UltraBook Pro arrived with a completely cracked screen. This is unacceptable. I need a replacement unit sent immediately.", TimeLabel="Mar 13, 8:30 AM" },
                            new TicketMessage { Sender="admin", Initials="JC", AvatarColor="#e8e8e8", Body="I am very sorry about this, Rodel. We have prioritized your case and initiated an express replacement request.", TimeLabel="Mar 13, 8:46 AM" }
                        }
                    }
                }
            };

            return View(viewModel);
        }

        // ── AJAX: Admin sends a reply ──
        // POST /Admin/HelpCenterReply
        [HttpPost]
        public IActionResult HelpCenterReply([FromBody] AdminReplyRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message))
                return BadRequest(new { success = false });

            // TODO: Save to database — _ticketService.AddReply(model.TicketId, model.Message, adminId);

            return Json(new
            {
                success = true,
                message = model.Message,
                timestamp = DateTime.Now.ToString("MMM dd, h:mm tt")
            });
        }

        // ── AJAX: Resolve a ticket ──
        // POST /Admin/HelpCenterResolve
        [HttpPost]
        public IActionResult HelpCenterResolve([FromBody] TicketActionRequest model)
        {
            if (model == null || model.TicketId <= 0)
                return BadRequest(new { success = false });

            // TODO: _ticketService.Resolve(model.TicketId);

            return Json(new { success = true, ticketId = model.TicketId });
        }

        // ── AJAX: Reopen a ticket ──
        // POST /Admin/HelpCenterReopen
        [HttpPost]
        public IActionResult HelpCenterReopen([FromBody] TicketActionRequest model)
        {
            if (model == null || model.TicketId <= 0)
                return BadRequest(new { success = false });

            // TODO: _ticketService.Reopen(model.TicketId);

            return Json(new { success = true, ticketId = model.TicketId });
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Dashboard");
        }
    }
}