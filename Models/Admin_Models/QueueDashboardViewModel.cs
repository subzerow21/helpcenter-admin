using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    // ── Page-level stats ──
    public class QueueDashboardStatsViewModel
    {
        public int InQueue { get; set; }
        public int ActiveSessions { get; set; }
        public string AvgWaitTime { get; set; } = "0:00";
        public int ResolvedToday { get; set; }
        public double AbandonRate { get; set; }
        public int AgentsOnline { get; set; }
    }

    // ── A single customer in the queue ──
    public class QueueSession
    {
        public int Id { get; set; }
        public string SessionNo { get; set; } = string.Empty;  // e.g. NH-00421
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Role { get; set; } = "Consumer";    // Consumer | Seller
        public string Initials { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = "#1a1a1a";
        public int WaitSeconds { get; set; }
        public int QueuePosition { get; set; }
        public string Status { get; set; } = "waiting";     // waiting | active | idle
        public string Category { get; set; } = "orders";
        public string? AssignedTo { get; set; }
        public List<QueueMessage> Messages { get; set; } = new();
    }

    // ── A single chat message ──
    public class QueueMessage
    {
        public string Sender { get; set; } = "cust";   // cust | agt | sys | note
        public string Body { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
    }

    // ── An agent ──
    public class AgentStatusViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Status { get; set; } = "online";  // online | busy | away | offline
        public int ActiveSessions { get; set; }
        public int MaxSessions { get; set; } = 3;
    }

    // ── Full page view model ──
    public class QueueDashboardViewModel
    {
        public QueueDashboardStatsViewModel Stats { get; set; } = new();
        public List<QueueSession> Sessions { get; set; } = new();
        public List<AgentStatusViewModel> Agents { get; set; } = new();
    }

    // ── POST: assign a session to an agent ──
    public class AssignSessionRequest
    {
        public int SessionId { get; set; }
        public string AgentName { get; set; } = string.Empty;
    }

    // ── POST: agent sends a message ──
    public class QueueSendRequest
    {
        public int SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsNote { get; set; } = false;
    }

    // ── POST: end or resolve a session ──
    public class QueueSessionActionRequest
    {
        public int SessionId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}