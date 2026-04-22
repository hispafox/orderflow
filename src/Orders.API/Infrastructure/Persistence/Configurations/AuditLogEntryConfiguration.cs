using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.API.Infrastructure.Audit;

namespace Orders.API.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogs", "orders");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.UserId)
               .HasDatabaseName("IX_AuditLogs_UserId");
        builder.HasIndex(e => e.Timestamp)
               .HasDatabaseName("IX_AuditLogs_Timestamp");
        builder.HasIndex(e => new { e.ResourceType, e.ResourceId })
               .HasDatabaseName("IX_AuditLogs_Resource");

        builder.Property(e => e.Action)       .HasMaxLength(100);
        builder.Property(e => e.UserId)       .HasMaxLength(100);
        builder.Property(e => e.UserEmail)    .HasMaxLength(320);
        builder.Property(e => e.ResourceType) .HasMaxLength(100);
        builder.Property(e => e.ResourceId)   .HasMaxLength(100);
        builder.Property(e => e.IpAddress)    .HasMaxLength(45);
        builder.Property(e => e.FailureReason).HasMaxLength(500);
    }
}
