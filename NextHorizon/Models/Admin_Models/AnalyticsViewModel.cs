using System.Collections.Generic;
using System.Linq;

namespace NextHorizon.Models.Admin_Models
{
    // --- ANALYTICS MODELS ---
    public class AnalyticsViewModel
    {
        public double ChallengeToSaleConversionRate { get; set; }
        public double AvgKmPerPurchase { get; set; }
        public int TotalSocialInteractions { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<AnalyticsChartData> PerformanceTrends { get; set; } = new();
        public List<RewardMetric> TopChallengeRewards { get; set; } = new();
        public List<SegmentMetric> SegmentScores { get; set; } = new();

        public int UsersStartedChallenge { get; set; }
        public int UsersCompletedGoal { get; set; }
        public int UsersClaimedReward { get; set; }
        public int UsersPurchased { get; set; }

        // New: Peak Activity Data (Hour 0-23)
        public List<int> ActivityByHour { get; set; } = new();

        // New: Seller Performance
        public List<SellerMetric> TopSellers { get; set; } = new();
    }

    public class AnalyticsChartData
    {
        public string DateLabel { get; set; } = string.Empty;
        public double TotalKm { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RewardMetric
    {
        public string ProductName { get; set; } = string.Empty;
        public string LinkedChallengeName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public string? ProductImageUrl { get; set; }
    }

    public class SegmentMetric
    {
        public string CategoryName { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
    }

    

    public class LeaderboardEntry
    {
        public string UserName { get; set; } = string.Empty;
        public double StravaKM { get; set; }
    }

    public class SellerMetric
    {
        public string SellerName { get; set; } = string.Empty;
        public int OrdersFulfilled { get; set; }
        public decimal RevenueGenerated { get; set; }
    }

}