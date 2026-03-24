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
        public string AthleteName { get; set; }
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public decimal TotalDistanceKm { get; set; }
        public int TotalActivities { get; set; }
        public int TotalTimeSeconds { get; set; }
        public string TotalTimeFormatted => TimeSpan.FromSeconds(TotalTimeSeconds).ToString(@"hh\:mm\:ss");
        public decimal AveragePace { get; set; }
        public string AveragePaceFormatted => $"{AveragePace:F2} min/km";
        public bool IsCompleted { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public string AvatarUrl { get; set; }
        public int ProgressPercent => TotalDistanceKm > 0 && ChallengeGoalKm > 0 
            ? (int)((TotalDistanceKm / ChallengeGoalKm) * 100) 
            : 0;
        
        // This will be set from the challenge
        public decimal ChallengeGoalKm { get; set; }
    }

    public class ActivityLogViewModel
    {
        public int ActivityId { get; set; }
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
}