using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    public class FinanceRequestsViewModel
    {
        public List<PayoutRequest> PendingPayouts { get; set; } = new List<PayoutRequest>();
        public List<DiscountRequest> PendingDiscounts { get; set; } = new List<DiscountRequest>();
        public decimal TotalPendingPayoutAmount { get; set; }
        public int TotalPendingDiscountsCount { get; set; }
    }

    public class PayoutRequest
    {
        public long Id { get; set; }  // Changed from WithdrawalId to Id for view compatibility
        public long WithdrawalId { get; set; }
        public int SellerId { get; set; }
        public int WalletId { get; set; }
        public int PayoutAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime DateRequested { get; set; }  // Changed from RequestedAt
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string SellerName { get; set; }
        public string ShopName { get; set; }
        public string SellerEmail { get; set; }
        public string BankName { get; set; }
    }

    public class DiscountRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string BannerSize { get; set; }
        public decimal TotalDiscountPercent { get; set; }
        public decimal DiscountPercent { get; set; }  
        public decimal TotalDiscountFix { get; set; }
        public int UsageLimit { get; set; }
        public bool UntilPromotionLast { get; set; }
        public int BuyQuantity { get; set; }
        public int TakeQuantity { get; set; }
        public string FreeItemRequirement { get; set; }
        public int ReturnWindowDays { get; set; }
        public string ReturnReasonsJson { get; set; }
        public string ReturnConditionRequirementsJson { get; set; }
        public string MinimumRequirementType { get; set; }
        public decimal MinimumPurchaseAmount { get; set; }
        public string Status { get; set; }
        public string SelectedProductIdsJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UserId { get; set; }
        public string ShopName { get; set; }
        public string ProductName { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ProductImage { get; set; }
    }

    public class GlobalPromotion
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public byte[] BannerImage { get; set; }  // Changed from string to byte array
        public string BannerImageName { get; set; }  // Original filename
        public string BannerImageContentType { get; set; }  // MIME type
        public string BannerImageBase64 { get; set; }  
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsIndefinite { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
    }

    public class SaveGlobalPromotionRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string BannerImageBase64 { get; set; }  // Add this property
        public string BannerImageName { get; set; } 
        public string BannerImageContentType { get; set; }  
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsIndefinite { get; set; }
    }
}