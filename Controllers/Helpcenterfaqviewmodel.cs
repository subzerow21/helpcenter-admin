using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    // ── FAQ item ──
    public class FaqItem
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = "general"; // orders|returns|payments|shipping|general
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    // ── Updated page view model ──
    public class HelpCenterV2ViewModel
    {
        public QueueDashboardStatsViewModel Stats { get; set; } = new();
        public List<QueueSession> Sessions { get; set; } = new();
        public List<AgentStatusViewModel> Agents { get; set; } = new();
        public List<FaqItem> Faqs { get; set; } = new();
    }

    // ── POST: Add FAQ ──
    public class AddFaqRequest
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = "general";
    }

    // ── POST: Update FAQ ──
    public class UpdateFaqRequest
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = "general";
    }

    // ── POST: Delete FAQ ──
    public class DeleteFaqRequest
    {
        public int Id { get; set; }
    }
}