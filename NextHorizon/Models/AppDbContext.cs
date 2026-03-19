using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;

namespace NextHorizon.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<StaffInfo> StaffInfo { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
                entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(50);
                entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(50);
                entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
                entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
                entity.Property(e => e.UserType).HasColumnName("user_type").HasMaxLength(20);
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LastLogin).HasColumnName("last_login");
            });

            // StaffInfo Configuration
            modelBuilder.Entity<StaffInfo>(entity =>
            {
                entity.ToTable("staff_info");
                entity.HasKey(e => e.StaffId);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                
                entity.Property(e => e.StaffId).HasColumnName("staff_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(50);
                entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(50);
                entity.Property(e => e.MiddleName).HasColumnName("middle_name").HasMaxLength(50);
                entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
                entity.Property(e => e.Permissions).HasColumnName("permissions");
                entity.Property(e => e.AddedBy).HasColumnName("added_by");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LastActive).HasColumnName("last_active");
                entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);

                // Relationships
                entity.HasOne(e => e.User)
                    .WithOne(u => u.StaffInfo)
                    .HasForeignKey<StaffInfo>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AddedByStaff)
                    .WithMany(e => e.Subordinates)
                    .HasForeignKey(e => e.AddedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // AuditLog Configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs");
                entity.HasKey(e => e.LogId);
                
                entity.Property(e => e.LogId).HasColumnName("log_id");
                entity.Property(e => e.Timestamp).HasColumnName("timestamp").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.StaffId).HasColumnName("staff_id");
                entity.Property(e => e.AdminName).HasColumnName("admin_name").HasMaxLength(100);
                entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(100);
                entity.Property(e => e.Target).HasColumnName("target").HasMaxLength(255);
                entity.Property(e => e.TargetType).HasColumnName("target_type").HasMaxLength(50);
                entity.Property(e => e.TargetId).HasColumnName("target_id");
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Success");
                entity.Property(e => e.Details).HasColumnName("details");
                entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasColumnName("user_agent");

                // Indexes
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.StaffId);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.TargetType);
                entity.HasIndex(e => e.Status);

                // Relationship
                entity.HasOne(e => e.Staff)
                    .WithMany(s => s.AuditLogs)
                    .HasForeignKey(e => e.StaffId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}