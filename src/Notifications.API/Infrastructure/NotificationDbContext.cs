using Microsoft.EntityFrameworkCore;
using Notifications.API.Domain;

namespace Notifications.API.Infrastructure;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.Id);

            entity.HasIndex(n => new { n.OrderId, n.Type })
                  .IsUnique()
                  .HasDatabaseName("IX_Notifications_OrderId_Type");

            entity.HasIndex(n => n.ProcessedAt)
                  .HasDatabaseName("IX_Notifications_ProcessedAt");

            entity.Property(n => n.Type).HasMaxLength(100);
            entity.Property(n => n.RecipientEmail).HasMaxLength(320);
        });
    }
}
