using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextHorizon.Models
{
    [Table("Agents", Schema = "dbo")]
    public class Agent
    {
        [Key]
        public int ChatID { get; set; }
        public int? ConversationID { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PreviewQuestion { get; set; } = string.Empty;
        public byte? ChatSlot { get; set; }        // tinyint
        public string ChatStatus { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string AgentStatus { get; set; } = string.Empty;
    }
}