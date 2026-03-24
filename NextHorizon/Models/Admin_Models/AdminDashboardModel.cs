using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    
 public class SuperAdminDashboardViewModel
{
    public PlatformStats Stats { get; set; }
    public List<TopSellerViewModel> TopSellers { get; set; }
    public List<ConsumerLeaderboardViewModel> ConsumerLeaderboard { get; set; }
     public List<DashboardApprovalItem> ApprovalHub { get; set; } = new();

   
    public decimal PlatformRevenue { get; set; }
    public int PendingPayouts { get; set; }
    public int PendingSellers { get; set; }
    public int PendingTickets { get; set; }
    public List<DashboardAuditLog> AuditLogs { get; set; } = new();
}


public class DashboardAuditLog
{
    public DateTime Timestamp { get; set; }
    public string AdminName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Target { get; set; } = "";
    public string Status { get; set; } = "";
}

    public class PlatformStats
    {
        public int TotalConsumers { get; set; }
        public int TotalSellers { get; set; }
        public int ActiveChallenges { get; set; }
        public string TotalKudos { get; set; } // String to handle "115.3K" formatting
    }

    public class TopSellerViewModel
    {
        public string ShopName { get; set; }
        public int MostPurchasedCount { get; set; } // Top 5 purchased items
        public int TotalProductsSold { get; set; }
        public string GrowthStatus { get; set; }
    }

    public class ConsumerLeaderboardViewModel
    {
        public int Rank { get; set; }
        public string UserName { get; set; }
        public decimal StravaKM { get; set; }
        public string Pace { get; set; }
        public bool IsVerified { get; set; }
    }

    public class SellerViewModel
    {
        public int SellerId { get; set; }
        public int UserId { get; set; }
        public string BusinessName { get; set; } = "";
        public string BusinessEmail { get; set; } = "";
        public string BusinessPhone { get; set; } = "";
        public string BusinessType { get; set; } = "";
        public string BusinessAddress { get; set; } = "";
        public string? LogoPath { get; set; }
        public string? DocumentPath { get; set; }
        public string SellerStatus { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public int TotalProducts { get; set; }      // ← add
        public decimal TotalSales { get; set; }     // ← add
    }


public class DashboardApprovalItem
{
    public string RequestType  { get; set; } = ""; // "New Seller", "Withdrawal", "Voucher"
    public string EntityName   { get; set; } = "";
    public string Details      { get; set; } = "";
    public string Status       { get; set; } = "";
    public string ActionLabel  { get; set; } = "";
    public string RedirectUrl  { get; set; } = "";
}
}
