using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.API.Infrastructure.Messaging;

public sealed class DemoEventLogConfiguration : IEntityTypeConfiguration<DemoEventLog>
{
    public void Configure(EntityTypeBuilder<DemoEventLog> builder)
    {
        builder.ToTable("DemoEventLog", "orders");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.Direction).HasMaxLength(16).IsRequired();
        builder.Property(e => e.MessageType).HasMaxLength(512).IsRequired();
        builder.Property(e => e.DestinationAddress).HasMaxLength(512);
        builder.Property(e => e.SourceAddress).HasMaxLength(512);
        builder.Property(e => e.ServiceName).HasMaxLength(64).IsRequired();

        builder.HasIndex(e => e.CorrelationId).HasDatabaseName("IX_DemoEventLog_CorrelationId");
        builder.HasIndex(e => e.OccurredAt).IsDescending().HasDatabaseName("IX_DemoEventLog_OccurredAt");
    }
}
