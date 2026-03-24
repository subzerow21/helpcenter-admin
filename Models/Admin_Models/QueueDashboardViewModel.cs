using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    public class QueueDashboardStatsViewModel
    {
        public int InQueue { get; set; }
        public int ActiveSessions { get; set; }
        public string AvgWaitTime { get; set; } = "0:00";
        public int ResolvedToday { get; set; }
        public int AgentsOnline { get; set; }
        public double AbandonRate { get; set; }
    }

    public class QueueSession
    {
        public int Id { get; set; }
        public string SessionNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Role { get; set; } = "Consumer";
        public string Initials { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = "#1a1a1a";
        public int WaitSeconds { get; set; }
        public int QueuePosition { get; set; }
        public string Status { get; set; } = "waiting";
        public string Category { get; set; } = "orders";
        public string? AssignedTo { get; set; }
        public List<QueueMessage> Messages { get; set; } = new();
    }

    public class QueueMessage
    {
        public string Sender { get; set; } = "user";
        public string Body { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
    }

    public class AgentStatusViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Status { get; set; } = "online";
        public int ActiveSessions { get; set; }
        public int MaxSessions { get; set; } = 3;
    }

    public class HelpCenterV2ViewModel
    {
        public QueueDashboardStatsViewModel Stats { get; set; } = new();
        public List<QueueSession> Sessions { get; set; } = new();
        public List<AgentStatusViewModel> Agents { get; set; } = new();
        public List<FaqItem> Faqs { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }

    public class AssignSessionRequest
    {
        public int SessionId { get; set; }
        public string AgentName { get; set; } = string.Empty;
    }

    public class QueueSendRequest
    {
        public int SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsNote { get; set; } = false;
    }

    public class QueueSessionActionRequest
    {
        public int SessionId { get; set; }
    }
}