using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;
namespace NextHorizon.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Faq> FAQs { get; set; }
        public DbSet<HelpSession> HelpSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            modelBuilder.Entity<HelpSession>(entity =>
            {
                entity.ToTable("SupportFAQs", "dbo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Category).HasColumnName("Category");
                entity.Property(e => e.Question).HasColumnName("Question");
                entity.Property(e => e.Resolution).HasColumnName("Resolution");
                entity.Property(e => e.DurationMinutes).HasColumnName("DurationMinutes");
                entity.Property(e => e.UserType).HasColumnName("UserType");
                entity.Property(e => e.AgentId).HasColumnName("AgentId");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            });
        }
    }
}