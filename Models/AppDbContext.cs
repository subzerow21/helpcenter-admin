using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;

namespace NextHorizon.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Faq> FAQs { get; set; }
        public DbSet<HelpSession> HelpSessions { get; set; }
        public DbSet<Agent> Agents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── FAQs ──────────────────────────────────────────────────────
            modelBuilder.Entity<Faq>(entity =>
            {
                entity.ToTable("FAQs", "dbo");
                entity.HasKey(e => e.FaqID);
                entity.Property(e => e.FaqID).HasColumnName("FaqID");
                entity.Property(e => e.Question).HasColumnName("Question");
                entity.Property(e => e.Answer).HasColumnName("Answer");
                entity.Property(e => e.Category).HasColumnName("Category");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.UserType).HasColumnName("UserType");
                entity.Property(e => e.user_id).HasColumnName("user_id");
                entity.Property(e => e.DateAdded).HasColumnName("DateAdded");
                entity.Property(e => e.LastUpdated).HasColumnName("LastUpdated");
            });

            // ── HelpSessions (SupportFAQs) ────────────────────────────────
            modelBuilder.Entity<HelpSession>(entity =>
            {
                entity.ToTable("SupportFAQs", "dbo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Category).HasColumnName("Category");
                entity.Property(e => e.Question).HasColumnName("Question");
                entity.Property(e => e.DurationMinutes).HasColumnName("DurationMinutes");
                entity.Property(e => e.UserType).HasColumnName("UserType");
                entity.Property(e => e.AgentId).HasColumnName("AgentId");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(e => e.StartTime).HasColumnName("StartTime");
                entity.Property(e => e.EndTime).HasColumnName("EndTime");
            });
            // ── Agents ────────────────────────────────────────────────────
            modelBuilder.Entity<Agent>(entity =>
            {
                entity.ToTable("Agents", "dbo");
                entity.HasKey(e => e.ChatID);
                entity.Property(e => e.ChatID).HasColumnName("ChatID");
                entity.Property(e => e.ConversationID).HasColumnName("ConversationID");
                entity.Property(e => e.AgentName).HasColumnName("AgentName");
                entity.Property(e => e.ClientName).HasColumnName("ClientName");
                entity.Property(e => e.Category).HasColumnName("Category");
                entity.Property(e => e.PreviewQuestion).HasColumnName("PreviewQuestion");
                entity.Property(e => e.ChatSlot).HasColumnName("ChatSlot");
                entity.Property(e => e.ChatStatus).HasColumnName("ChatStatus");
                entity.Property(e => e.AgentStatus).HasColumnName("AgentStatus");
                entity.Property(e => e.AgentID).HasColumnName("AgentID");  
                entity.Property(e => e.UserID).HasColumnName("UserID");  
            });
        }
    }
}