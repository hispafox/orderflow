using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Payments.API.Infrastructure.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData     eventData,
        InterceptionResult<int> result,
        CancellationToken      ct = default)
    {
        if (eventData.Context is null) return new(result);

        var now = DateTime.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.State == EntityState.Added
                && entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
            {
                entry.Property("CreatedAt").CurrentValue = now;
            }

            if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                entry.Property("UpdatedAt").CurrentValue = now;
        }

        return new(result);
    }
}
