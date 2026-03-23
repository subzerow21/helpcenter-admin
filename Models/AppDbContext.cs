using Microsoft.EntityFrameworkCore;
using NextHorizon.Models.Admin_Models;

namespace NextHorizon.Models.Admin_Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<FaqItem> FAQs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FaqItem>(entity =>
            {
                entity.ToTable("FAQs", "dbo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FaqID");
                entity.Property(e => e.Question).HasColumnName("Question");
                entity.Property(e => e.Answer).HasColumnName("Answer");
                entity.Property(e => e.Category).HasColumnName("Category");
                entity.Property(e => e.CreatedAt).HasColumnName("DateAdded");
                entity.Property(e => e.UpdatedAt).HasColumnName("LastUpdated");
            });
        }
    }
}