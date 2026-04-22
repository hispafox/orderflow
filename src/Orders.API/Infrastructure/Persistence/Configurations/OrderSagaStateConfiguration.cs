using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.API.Sagas;

namespace Orders.API.Infrastructure.Persistence.Configurations;

public class OrderSagaStateConfiguration : IEntityTypeConfiguration<OrderSagaState>
{
    public void Configure(EntityTypeBuilder<OrderSagaState> builder)
    {
        builder.ToTable("OrderSagaState", "orders");
        builder.HasKey(s => s.CorrelationId);

        builder.Property(s => s.CurrentState)  .HasMaxLength(64);
        builder.Property(s => s.CustomerEmail) .HasMaxLength(320);
        builder.Property(s => s.Currency)      .HasMaxLength(3);
        builder.Property(s => s.FailureReason) .HasMaxLength(500);
        builder.Property(s => s.Amount)        .HasPrecision(18, 2);

        builder.Property(s => s.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        builder.Property(s => s.ReservationTimeoutTokenId)
               .IsRequired(false);
    }
}
