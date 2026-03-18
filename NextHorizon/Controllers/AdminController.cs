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
            // Define raw product data for the "Most Purchased" section
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
                // Top Header Cards
                TotalConsumers = 12450,
                TotalSellers = 84,
                TotalRevenue = 7954000m,
                AverageOrderValue = 4850.00,
                ChallengeToSaleConversionRate = 32.8,
                UsersPurchased = 1640,
                TotalOrders = 1240,

                // Main Area Chart Data
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

                // Sidebar: Top Sellers (Take 5 for the small list)
                TopSellers = new List<SellerMetric>
                {
                    new SellerMetric { Rank = 1, SellerName = "Elite Strides Ph", ShopName = "Elite Sports Store", OrdersFulfilled = 452, RevenueGenerated = 1254000.50m },
                    new SellerMetric { Rank = 2, SellerName = "Mountain Peak Gear", ShopName = "Peak Adventure Shop", OrdersFulfilled = 310, RevenueGenerated = 890600.00m },
                    new SellerMetric { Rank = 3, SellerName = "Urban Runner Co.", ShopName = "Urban Runner Flagship", OrdersFulfilled = 285, RevenueGenerated = 412000.75m },
                    new SellerMetric { Rank = 4, SellerName = "Velocity Sports", ShopName = "Velocity Metro", OrdersFulfilled = 198, RevenueGenerated = 356000.00m },
                    new SellerMetric { Rank = 5, SellerName = "HydroFlow Official", ShopName = "HydroFlow PH", OrdersFulfilled = 156, RevenueGenerated = 98000.25m }
                }.OrderByDescending(s => s.RevenueGenerated).ToList(),

                // Bottom: Most Purchased Products
                TopMovingProducts = productData.OrderByDescending(p => p.UnitsSold).Take(5).ToList(),

                // Peak Activity Chart Data
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

        // URL: /Admin/SellerPerformance
        public IActionResult SellerPerformance()
        {
            // Detailed Seller List for Table
            var mockSellers = new List<SellerMetric>
            {
                new SellerMetric { Rank = 1, SellerName = "Nike Official", ShopName = "Nike PH Store", OrdersFulfilled = 1450, RevenueGenerated = 850400.00m, GrowthPercentage = 12.5 },
                new SellerMetric { Rank = 2, SellerName = "Adidas Philippines", ShopName = "Adidas PH Store", OrdersFulfilled = 1200, RevenueGenerated = 720300.50m, GrowthPercentage = 8.2 },
                new SellerMetric { Rank = 3, SellerName = "Under Armour", ShopName = "UA Performance Center", OrdersFulfilled = 890, RevenueGenerated = 450000.75m, GrowthPercentage = 15.0 },
                new SellerMetric { Rank = 4, SellerName = "Titan 22", ShopName = "Titan Basketball", OrdersFulfilled = 760, RevenueGenerated = 310200.20m, GrowthPercentage = 5.4 },
                new SellerMetric { Rank = 5, SellerName = "Puma Metro", ShopName = "Puma Metro Hub", OrdersFulfilled = 540, RevenueGenerated = 120500.00m, GrowthPercentage = -2.1 }
            };

            // Detailed Product List for Tab 2
            var mockProducts = new List<ProductMetric>
            {
                new ProductMetric { ProductName = "Vaporfly Next%", Category = "Running", SalesCount = 320, Revenue = 1500000m },
                new ProductMetric { ProductName = "Yoga Mat Pro", Category = "Fitness", SalesCount = 280, Revenue = 56000m },
                new ProductMetric { ProductName = "Dumbbell Set 10kg", Category = "Fitness", SalesCount = 150, Revenue = 75000m },
                new ProductMetric { ProductName = "Stainless Water Bottle", Category = "Accessories", SalesCount = 410, Revenue = 92000m },
                new ProductMetric { ProductName = "Performance Socks", Category = "Apparel", SalesCount = 520, Revenue = 25000m }
             }.OrderByDescending(p => p.Revenue).ToList();

            var viewModel = new AnalyticsViewModel
            {
                TotalSellers = 124,
                TotalConsumers = 1540,
                TotalRevenue = 2451401.45m, // Drives the Donut Chart Total
                TotalOrders = 5840,
                TopSellers = mockSellers,
                TopProducts = mockProducts // Drives the "Top Moving Products" tab
            };

            return View(viewModel);
        }

        public IActionResult Consumers() => View();
        public IActionResult Sellers() => View();

        public IActionResult FinanceRequest()
        {
            var viewModel = new FinanceRequestsViewModel
            {
                PendingPayouts = new List<PayoutRequest>
                {
                    new PayoutRequest { Id = "1001", ShopName = "TechGear Hub", SellerEmail = "owner@techgear.com", Amount = 15500.50m, BankName = "GCash", SellerNote = "Funds Transfer", DateRequested = DateTime.Now.AddDays(-2) },
                    new PayoutRequest { Id = "1002", ShopName = "Luxe Apparel", SellerEmail = "sales@luxe.ph", Amount = 42000.00m, BankName = "BDO", SellerNote = "Withdrawal", DateRequested = DateTime.Now.AddDays(-1) }
                },
                PendingDiscounts = new List<DiscountRequest>
                {
                    new DiscountRequest { Id = "D-55", ShopName = "TechGear Hub", ProductName = "Wireless Earbuds Pro", OriginalPrice = 2500, DiscountPercent = 20, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(7) },
                    new DiscountRequest { Id = "D-56", ShopName = "Home Essentials", ProductName = "Ergonomic Chair", OriginalPrice = 8999, DiscountPercent = 50, StartDate = DateTime.Now.AddDays(2), EndDate = DateTime.Now.AddDays(5) }
                }
            };

            return View("FinanceRequest", viewModel);
        }

        public IActionResult Logistics() => View();
        public IActionResult Tasks() => View();
        public IActionResult ChallengeDetails() => View();
        // URL: /Admin/HelpCenter
        public IActionResult HelpCenter()
        {
            return View();
        }

        public IActionResult Settings() => View();
        public IActionResult Notifications() => View();

        public IActionResult Logout()
        {
            // Redirect back to Dashboard for now
            return RedirectToAction("Dashboard");
        }
    }
}