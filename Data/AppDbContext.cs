using Megdan.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Megdan.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<ComplaintComment> ComplaintComments => Set<ComplaintComment>();
        public DbSet<ComplaintImage> ComplaintImages => Set<ComplaintImage>();
        public DbSet<ComplaintFeedback> ComplaintFeedbacks => Set<ComplaintFeedback>();
        public DbSet<TrackingToken> TrackingTokens => Set<TrackingToken>();
        public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
        public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
        public DbSet<EmailAccessToken> EmailAccessTokens => Set<EmailAccessToken>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.Feedback)
                .WithOne(f => f.Complaint)
                .HasForeignKey<ComplaintFeedback>(f => f.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrackingToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            modelBuilder.Entity<Complaint>()
                .Property(c => c.Latitude)
                .HasPrecision(9, 6);

            modelBuilder.Entity<Complaint>()
                .Property(c => c.Longitude)
                .HasPrecision(9, 6);

            modelBuilder.Entity<NotificationLog>()
                .HasOne(n => n.Complaint)
                .WithMany()
                .HasForeignKey(n => n.ComplaintId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EmailAccessToken>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(x => x.Token)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(x => x.Token)
                    .IsUnique();
            });
        }
    }
}