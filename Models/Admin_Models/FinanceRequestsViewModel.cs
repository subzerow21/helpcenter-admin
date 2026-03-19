using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    public class FinanceRequestsViewModel
    {
        // Lists for the two tabs
        public List<PayoutRequest> PendingPayouts { get; set; } = new List<PayoutRequest>();
        public List<DiscountRequest> PendingDiscounts { get; set; } = new List<DiscountRequest>();

        // Optional: Summary stats for the top of the page
        public decimal TotalPendingPayoutAmount { get; set; }
        public int TotalPendingDiscountsCount { get; set; }
    }

    public class PayoutRequest
    {
        public string Id { get; set; }
        public string ShopName { get; set; }
        public string SellerEmail { get; set; }
        public decimal Amount { get; set; }
        public string BankName { get; set; } // e.g., GCash, BDO, Maya

        public String SellerNote { get; set; }
        public DateTime DateRequested { get; set; }
        public string Status { get; set; } // Pending, Approved, Rejected
    }

    public class DiscountRequest
    {
        public string Id { get; set; }

        public string? ProductImage { get; set; }
        public string ShopName { get; set; }
        public string ProductName { get; set; }
        public decimal OriginalPrice { get; set; }
        public int DiscountPercent { get; set; }
        public decimal DiscountedPrice => OriginalPrice - (OriginalPrice * DiscountPercent / 100);
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}