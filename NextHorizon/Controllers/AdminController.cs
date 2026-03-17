using Microsoft.AspNetCore.Mvc;
using NextHorizon.Models.Admin_Models;
using NextHorizon.Services.AdminServices;
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

        public IActionResult Analytics()
        {
            // Define the raw data first
            var productData = new List<ProductMetric>
    {
        new ProductMetric { ProductName = "UltraBoost v2", UnitsSold = 142, Revenue = 852000m },
        new ProductMetric { ProductName = "Hydration Pack", UnitsSold = 98, Revenue = 147000m },
        new ProductMetric { ProductName = "Swift Shorts", UnitsSold = 76, Revenue = 38000m },
        new ProductMetric { ProductName = "Pro GPS Watch", UnitsSold = 185, Revenue = 675000m }, // Increased units to test sorting
        new ProductMetric { ProductName = "Compression Socks", UnitsSold = 210, Revenue = 42000m }
    };

            var viewModel = new AnalyticsViewModel
            {
                // Growth Metrics
                TotalConsumers = 12450,
                TotalSellers = 84,
                TotalRevenue = 7954000m,
                AverageOrderValue = 4850.00,
                ChallengeToSaleConversionRate = 32.8,
                UsersPurchased = 1640,

                PerformanceTrends = new List<AnalyticsChartData>
                {
                    new AnalyticsChartData { DateLabel = "Mar 01", ChallengeParticipants = 120, TotalRevenue = 110000m },
                    new AnalyticsChartData { DateLabel = "Mar 02", ChallengeParticipants = 150, TotalRevenue = 135000m },
                    new AnalyticsChartData { DateLabel = "Mar 03", ChallengeParticipants = 95,  TotalRevenue = 95000m },
                    new AnalyticsChartData { DateLabel = "Mar 04", ChallengeParticipants = 210, TotalRevenue = 160000m },
                    new AnalyticsChartData { DateLabel = "Mar 05", ChallengeParticipants = 300, TotalRevenue = 210000m },
                    new AnalyticsChartData { DateLabel = "Mar 06", ChallengeParticipants = 280, TotalRevenue = 195000m },
                    new AnalyticsChartData { DateLabel = "Mar 07", ChallengeParticipants = 350, TotalRevenue = 240000m }
                },

                // Top Performing Sellers (Sorted by Revenue)
                TopSellers = new List<SellerMetric>
                {
                    new SellerMetric { SellerName = "Elite Strides Ph", OrdersFulfilled = 452, RevenueGenerated = 1254000.50m },
                    new SellerMetric { SellerName = "Mountain Peak Gear", OrdersFulfilled = 310, RevenueGenerated = 890600.00m },
                    new SellerMetric { SellerName = "Urban Runner Co.", OrdersFulfilled = 285, RevenueGenerated = 412000.75m },
                    new SellerMetric { SellerName = "Velocity Sports", OrdersFulfilled = 198, RevenueGenerated = 356000.00m },
                    new SellerMetric { SellerName = "HydroFlow Official", OrdersFulfilled = 156, RevenueGenerated = 98000.25m }
                }.OrderByDescending(s => s.RevenueGenerated).ToList(),

                // Most Purchased Products (Arranged by Units Sold)
                TopMovingProducts = productData.OrderByDescending(p => p.UnitsSold).Take(5).ToList(),

                // Peak Engagement Data (Full 24-hour cycle sample)
                PeakEngagementData = new List<HourlyEngagementMetric>
                {
                    new HourlyEngagementMetric { Hour = 0, ActivitySyncCount = 20, PurchaseCount = 5 },
                    new HourlyEngagementMetric { Hour = 6, ActivitySyncCount = 420, PurchaseCount = 15 },
                    new HourlyEngagementMetric { Hour = 9, ActivitySyncCount = 150, PurchaseCount = 40 },
                    new HourlyEngagementMetric { Hour = 12, ActivitySyncCount = 110, PurchaseCount = 85 },
                    new HourlyEngagementMetric { Hour = 17, ActivitySyncCount = 580, PurchaseCount = 60 },
                    new HourlyEngagementMetric { Hour = 21, ActivitySyncCount = 310, PurchaseCount = 190 }
                }.OrderBy(h => h.Hour).ToList()
            };

            return View(viewModel);
        }

        public IActionResult SellerPerformance()
        {
            var mockSellers = new List<SellerMetric>
            {
                new SellerMetric { Rank = 1, SellerName = "Nike Official", OrdersFulfilled = 1450, RevenueGenerated = 850400.00m, GrowthPercentage = 12.5 },
                new SellerMetric { Rank = 2, SellerName = "Adidas Philippines", OrdersFulfilled = 1200, RevenueGenerated = 720300.50m, GrowthPercentage = 8.2 },
                new SellerMetric { Rank = 3, SellerName = "Under Armour", OrdersFulfilled = 890, RevenueGenerated = 450000.75m, GrowthPercentage = 15.0 },
                new SellerMetric { Rank = 4, SellerName = "Titan 22", OrdersFulfilled = 760, RevenueGenerated = 310200.20m, GrowthPercentage = 5.4 },
                new SellerMetric { Rank = 5, SellerName = "Puma Metro", OrdersFulfilled = 540, RevenueGenerated = 120500.00m, GrowthPercentage = -2.1 }
            };

            var viewModel = new AnalyticsViewModel
            {
                TotalSellers = 124,
                TotalConsumers = 1540,
                TopSellers = mockSellers
            };

            return View(viewModel);
        }


        public IActionResult Consumers()
        {
            return View();
        }

        public IActionResult Sellers()
        {
            return View();
        }

        public IActionResult FinanceRequest()
        {
            var viewModel = new FinanceRequestsViewModel
            {
                // Mock Payouts
                PendingPayouts = new List<PayoutRequest>
                {
                    new PayoutRequest { Id = "1001", ShopName = "TechGear Hub", SellerEmail = "owner@techgear.com", Amount = 15500.50m, BankName = "GCash",  SellerNote = "Funds Transfer", DateRequested = DateTime.Now.AddDays(-2) },
                    new PayoutRequest { Id = "1002", ShopName = "Luxe Apparel", SellerEmail = "sales@luxe.ph", Amount = 42000.00m, BankName = "BDO",  SellerNote = "Witdrawal", DateRequested = DateTime.Now.AddDays(-1) }
                },

                        // Mock Discounts
                        PendingDiscounts = new List<DiscountRequest>
                {
                    new DiscountRequest { Id = "D-55", ShopName = "TechGear Hub", ProductName = "Wireless Earbuds Pro", OriginalPrice = 2500, DiscountPercent = 20, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(7) },
                    new DiscountRequest { Id = "D-56", ShopName = "Home Essentials", ProductName = "Ergonomic Chair", OriginalPrice = 8999, DiscountPercent = 50, StartDate = DateTime.Now.AddDays(2), EndDate = DateTime.Now.AddDays(5) }
                }
            };

            return View("FinanceRequest", viewModel);
        }
        public IActionResult Logistics()
        {
            return View();
        }
        public IActionResult Tasks()
        {
            return View();
        }

        public IActionResult ChallengeDetails()
        {
            return View();
        }

        public IActionResult Moderation()
        {
            return View();
        }

        public IActionResult HelpCenter()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }


        public IActionResult Logout()
        {

            //modify later to log out the user and redirect to login page
            return View();
        }
    }
}