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
            var viewModel = new AnalyticsViewModel
            {
                // Existing Summary Metrics
                ChallengeToSaleConversionRate = 32.8,
                AvgKmPerPurchase = 12.4,
                TotalSocialInteractions = 4200000,
                AverageOrderValue = 4850.00m,

                // Funnel Data (New)
                UsersStartedChallenge = 5000,
                UsersCompletedGoal = 3200,
                UsersClaimedReward = 2100,
                UsersPurchased = 1640,

                // Performance Trends (Chart 1)
                PerformanceTrends = new List<AnalyticsChartData>
        {
            new AnalyticsChartData { DateLabel = "Mar 04", TotalKm = 1500, TotalRevenue = 160000 },
            new AnalyticsChartData { DateLabel = "Mar 05", TotalKm = 1800, TotalRevenue = 210000 },
            new AnalyticsChartData { DateLabel = "Mar 06", TotalKm = 1650, TotalRevenue = 195000 },
            new AnalyticsChartData { DateLabel = "Mar 07", TotalKm = 2100, TotalRevenue = 240000 }
        },

                // Top Performing Sellers (New)
                TopSellers = new List<SellerMetric>
        {
            new SellerMetric { SellerName = "Elite Strides Ph", OrdersFulfilled = 452, RevenueGenerated = 1254000.50m },
            new SellerMetric { SellerName = "Mountain Peak Gear", OrdersFulfilled = 310, RevenueGenerated = 890600.00m },
            new SellerMetric { SellerName = "Urban Runner Co.", OrdersFulfilled = 285, RevenueGenerated = 412000.75m },
            new SellerMetric { SellerName = "Velocity Sports", OrdersFulfilled = 198, RevenueGenerated = 356000.00m },
            new SellerMetric { SellerName = "HydroFlow Official", OrdersFulfilled = 156, RevenueGenerated = 98000.25m }
        },

                // Reward Metrics
                TopChallengeRewards = new List<RewardMetric>
        {
            new RewardMetric { ProductName = "Ultra Boost Z", LinkedChallengeName = "Ironman Monthly", UnitsSold = 842 },
            new RewardMetric { ProductName = "Pro GPS Watch", LinkedChallengeName = "Summit Climbers", UnitsSold = 520 }
        },

                // Segment Engagement
                SegmentScores = new List<SegmentMetric>
        {
            new SegmentMetric { CategoryName = "Elite Runners", ProgressPercentage = 94 },
            new SegmentMetric { CategoryName = "Urban Commuters", ProgressPercentage = 78 },
            new SegmentMetric { CategoryName = "Trail Explorers", ProgressPercentage = 56 }
        }
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