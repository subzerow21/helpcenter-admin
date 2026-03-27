using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    public class ChallengeViewModel
    {
        public int ChallengeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Rules { get; set; }
        public string Prizes { get; set; }
        public decimal GoalKm { get; set; }
        public string ActivityType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string BannerBase64 { get; set; }
        public string BannerImageName { get; set; }
        public string BannerImageContentType { get; set; }
        public int TotalParticipants { get; set; }
        public int TotalCompleted { get; set; }
        public decimal CompletionRate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public byte[] BannerImageBinary { get; set; }
        
        // Display properties
        public string StatusBadgeClass => Status switch
        {
            "Live" => "bg-success",
            "Upcoming" => "bg-warning",
            "Completed" => "bg-secondary",
            "Cancelled" => "bg-danger",
            _ => "bg-secondary"
        };
        
        public string DateRange => $"{StartDate:MMM dd, yyyy} - {EndDate:MMM dd, yyyy}";
    }

    public class ChallengeDetailsViewModel
    {
        public ChallengeViewModel Challenge { get; set; }
        public List<ParticipantLeaderboard> Leaderboard { get; set; }
        public decimal AvgDistanceKm { get; set; }
        public decimal AvgTimeMinutes { get; set; }
    }

    public class ParticipantLeaderboard
    {    
        public int Rank { get; set; }  
        public int ParticipantId { get; set; }
        public int UserId { get; set; }
        public int ConsumerId { get; set; }
        public string AthleteName { get; set; }
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public decimal TotalDistanceKm { get; set; }
        public int TotalActivities { get; set; }
        public int TotalTimeSeconds { get; set; }
        public string TotalTimeFormatted { get; set; }
        public decimal AveragePace { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? StoredRank { get; set; }  // The rank stored in challenge_participants table
        public string AvatarUrl { get; set; }
        public decimal ChallengeGoalKm { get; set; }
        public decimal ProgressPercent { get; set; }
    }

    public class ActivityLogViewModel
    {
        public int ActivityId { get; set; }
        public int ParticipantId { get; set; }        // Add this - needed for tracking
         public int ChallengeId { get; set; }
        public DateTime ActivityDate { get; set; }
        public decimal DistanceKm { get; set; }
        public int DurationSeconds { get; set; }
        public string DurationFormatted => TimeSpan.FromSeconds(DurationSeconds).ToString(@"hh\:mm\:ss");
        public decimal AveragePace { get; set; }
        public string AveragePaceFormatted => $"{AveragePace:F2} min/km";
        public string ActivityType { get; set; }
        public bool IsVerified { get; set; }
        public int? VerifiedBy { get; set; }
        public string VerifiedByName { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ChallengeTitle { get; set; }
        public string ImageProofBase64 { get; set; }  
        public byte[] ImageProof { get; set; }
    }

    public class CreateChallengeRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Rules { get; set; }
        public string Prizes { get; set; }
        public decimal GoalKm { get; set; }
        public string ActivityType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string BannerBase64 { get; set; }
        public string BannerImageName { get; set; }
        public string BannerImageContentType { get; set; }
        public List<PrizeData> PrizesData { get; set; } // Add this
    }

    public class UpdateChallengeRequest
    {
        public int ChallengeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Rules { get; set; }
        public string Prizes { get; set; }
        public decimal GoalKm { get; set; }
        public string ActivityType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string BannerBase64 { get; set; }
        public string BannerImageName { get; set; }
        public string BannerImageContentType { get; set; }
        public List<PrizeData> PrizesData { get; set; } 
    }

    public class PrizeData
    {
        public int Tier { get; set; }
        public string TierName { get; set; }
        public string Description { get; set; }
        public int PrizeTypeId { get; set; }
        public decimal? CashAmount { get; set; }
        public decimal? VoucherDiscountPercent { get; set; }
        public decimal? VoucherDiscountFixed { get; set; }
        public decimal? VoucherMinimumPurchase { get; set; }
        public string VoucherType { get; set; }
        public string RewardName { get; set; }
        public decimal? RewardValue { get; set; }
        public int Quantity { get; set; }
    }

    public class AddActivityRequest
    {
        public int ChallengeId { get; set; }
        public DateTime ActivityDate { get; set; }
        public decimal DistanceKm { get; set; }
        public int DurationSeconds { get; set; }
        public string ActivityType { get; set; }
        public string Notes { get; set; }
        public bool AutoVerify { get; set; }
    }

    public class VerifyActivityRequest
    {
        public int ActivityId { get; set; }
    }

    public class ChallengeStatistics
    {
        public int TotalAthletes { get; set; }
        public int ActiveChallenges { get; set; }
        public decimal AvgDistance { get; set; }
        public decimal TotalTimeHours { get; set; }
    }

    public class PrizeType
    {
        public int PrizeTypeId { get; set; }
        public string TypeName { get; set; }
        public string TypeCode { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class ChallengePrize
    {
        public int PrizeId { get; set; }
        public int ChallengeId { get; set; }
        public int PrizeTypeId { get; set; }
        public int Tier { get; set; }
        public string TierName { get; set; }
        public string Description { get; set; }
        
        // Cash
        public decimal? CashAmount { get; set; }
        
        // Voucher
        public decimal? VoucherDiscountPercent { get; set; }
        public decimal? VoucherDiscountFixed { get; set; }
        public decimal? VoucherMinimumPurchase { get; set; }
        public string VoucherType { get; set; }
        
        // Reward
        public string RewardName { get; set; }
        public decimal? RewardValue { get; set; }
        public int? RewardQuantity { get; set; }
        
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
    }

    public class PrizeWinner
    {
        public int WinnerId { get; set; }
        public int PrizeId { get; set; }
        public int ParticipantId { get; set; }
        public int ChallengeId { get; set; }
        public int RankPosition { get; set; }
        
        public string ClaimStatus { get; set; }
        public DateTime? ClaimDate { get; set; }
        public DateTime? ClaimDeadline { get; set; }
        public string ClaimNotes { get; set; }
        
        public string ClaimCode { get; set; }
        public DateTime? ClaimCodeGeneratedAt { get; set; }
        
        // Cash
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string TransactionReference { get; set; }
        public DateTime? TransferDate { get; set; }
        
        // Voucher
        public string VoucherCode { get; set; }
        public DateTime? VoucherExpiry { get; set; }
        public DateTime? VoucherSentDate { get; set; }
        public DateTime? VoucherRedemptionDate { get; set; }
        public bool VoucherUsed { get; set; }
        
        // Physical Reward
        public string ShippingAddress { get; set; }
        public string TrackingNumber { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        
        // Digital Reward
        public string DigitalCode { get; set; }
        public DateTime? DigitalCodeSentDate { get; set; }
        public bool DigitalCodeUsed { get; set; }
        
        public int? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string RejectionReason { get; set; }
        public string AdminNotes { get; set; }
    }

    // Request Models
    public class CreatePrizeRequest
    {
        public int ChallengeId { get; set; }
        public int PrizeTypeId { get; set; }
        public int Tier { get; set; }
        public string TierName { get; set; }
        public string Description { get; set; }
        public decimal? CashAmount { get; set; }
        public decimal? VoucherDiscountPercent { get; set; }
        public decimal? VoucherDiscountFixed { get; set; }
        public decimal? VoucherMinimumPurchase { get; set; }
        public string VoucherType { get; set; }
        public string RewardName { get; set; }
        public decimal? RewardValue { get; set; }
        public int? RewardQuantity { get; set; }
        public int Quantity { get; set; }
    }

    public class ClaimPrizeRequest
    {
        public string ClaimCode { get; set; }
        public string ClaimDetails { get; set; } // JSON string
    }

    public class ProcessPrizeRequest
    {
        public int WinnerId { get; set; }
        public string Action { get; set; } // approve, reject
        public string RejectionReason { get; set; }
        public string AdminNotes { get; set; }
        
        // For approved prizes
        public string VoucherCode { get; set; }
        public DateTime? VoucherExpiry { get; set; }
        public string TrackingNumber { get; set; }
        public string DigitalCode { get; set; }
        public string TransactionReference { get; set; }
    }
    public class GlobalLeaderboardEntry
    {
        public int GlobalRank { get; set; }
        public int ParticipantId { get; set; }
        public int UserId { get; set; }
        public int ConsumerId { get; set; }
        public int ChallengeId { get; set; }
        public string ChallengeTitle { get; set; }
        public string ActivityType { get; set; }
        public decimal ChallengeGoalKm { get; set; }
        public string AthleteName { get; set; }
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public decimal TotalDistanceKm { get; set; }
        public int TotalActivities { get; set; }
        public int TotalTimeSeconds { get; set; }
        public string TotalTimeFormatted { get; set; }
        public decimal AveragePace { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? ChallengeRank { get; set; }
        public decimal ProgressPercent { get; set; }
        public string AvatarUrl { get; set; }
    }
}