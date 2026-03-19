using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    public class HelpCenterStatsViewModel
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int PendingTickets { get; set; }
        public int UrgentTickets { get; set; }
        public int ResolvedToday { get; set; }
        public string AvgResponse { get; set; } = "0h";
    }

    public class SupportTicket
    {
        public int Id { get; set; }
        public string TicketNo { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = "#1a1a1a";
        public string Status { get; set; } = "open";
        public string Category { get; set; } = "orders";
        public string DateLabel { get; set; } = string.Empty;
        public string Role { get; set; } = "Consumer";
        public List<TicketMessage> Messages { get; set; } = new();
    }

    public class TicketMessage
    {
        public string Sender { get; set; } = "user";
        public string Initials { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = "#1a1a1a";
        public string Body { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
        public bool IsResolved { get; set; } = false;
    }

    public class HelpCenterViewModel
    {
        public HelpCenterStatsViewModel Stats { get; set; } = new();
        public List<SupportTicket> Tickets { get; set; } = new();
    }

    public class AdminReplyRequest
    {
        public int TicketId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TicketActionRequest
    {
        public int TicketId { get; set; }
    }
}