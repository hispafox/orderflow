using Microsoft.EntityFrameworkCore;
using Notifications.API.Infrastructure;

namespace Notifications.API.Tests.Shared;

public class NotificationsLocalDbFixture : IAsyncLifetime
{
    private readonly string _testDbName = $"NotificationsTestDb_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;" +
        $"Database={_testDbName};" +
        $"Trusted_Connection=true;" +
        $"TrustServerCertificate=true;";

    public NotificationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new NotificationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition("NotificationsCollection")]
public class NotificationsCollection : ICollectionFixture<NotificationsLocalDbFixture> { }
