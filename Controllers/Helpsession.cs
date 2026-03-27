using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextHorizon.Models
{
    [Table("SupportFAQs", Schema = "dbo")]
    public class HelpSession
    {
        [Key]
        public int Id { get; set; }

        public string Category { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string? Resolution { get; set; }
        public int DurationMinutes { get; set; }
        public string UserType { get; set; } = "Consumer";
        public int? AgentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}