using System.Collections.Generic;
using System.Linq;

namespace NextHorizon.Models.Admin_Models
{
    public class AnalyticsViewModel
    {
        // --- Growth Stats ---
        public int TotalConsumers { get; set; }
        public int TotalSellers { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageOrderValue { get; set; }
        public double ChallengeToSaleConversionRate { get; set; }
        public int UsersPurchased { get; set; }

        // --- Lists & Trends ---
        public List<AnalyticsChartData> PerformanceTrends { get; set; } = new();
        public List<SellerMetric> TopSellers { get; set; } = new();

        // --- Product Performance ---
        public List<ProductMetric> TopMovingProducts { get; set; } = new();

        // --- Hourly Engagement (Strava Syncs vs. Purchases) ---
        public List<HourlyEngagementMetric> PeakEngagementData { get; set; } = new();
    }

    // --- Supporting Metric Classes ---

    public class AnalyticsChartData
    {
        public string DateLabel { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int ChallengeParticipants { get; set; } // New Metric
    }

    public class ProductMetric
    {
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class HourlyEngagementMetric
    {
        public int Hour { get; set; } // 0 to 23
        public int ActivitySyncCount { get; set; }
        public int PurchaseCount { get; set; }
    }

    public class SellerMetric
    {
        public int Rank { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public int OrdersFulfilled { get; set; }
        public decimal RevenueGenerated { get; set; }
        public double GrowthPercentage { get; set; }
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string UserName { get; set; } = string.Empty;
        public double StravaKM { get; set; }
        public string Pace { get; set; } = "0:00";
    }
}