using Megdan.Domain.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Megdan.Domain.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure BaseEntity (Guid as Primary Key)
            modelBuilder.Entity<Complaint>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Comment>()
                .HasKey(c => c.Id);

            // Per Change Request CR-001: 
            // Ensure TrackingToken is unique for guest tracking
            modelBuilder.Entity<Complaint>()
                .HasIndex(c => c.TrackingToken)
                .IsUnique();

            // Configure the One-to-Many Relationship
            // One Complaint has many Comments
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Complaint)
                .WithMany(c => c.Comments)
                .HasForeignKey(c => c.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            // Per Project Specification: 
            // Set precision for coordinates if using decimals, 
            // or ensure they are mapped correctly as doubles.
            modelBuilder.Entity<Complaint>()
                .Property(c => c.Latitude)
                .IsRequired();

            modelBuilder.Entity<Complaint>()
                .Property(c => c.Longitude)
                .IsRequired();

            // Seed initial categories or status if necessary
            // (Optional: can be handled via migrations)
        }

    }
}
