using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.API.Infrastructure.Persistence;
using Orders.API.Tests.Shared.Fixtures;
using Xunit;

namespace Orders.API.Tests.Integration.Migrations;

[Collection("OrdersApiCollection")]
[Trait("Category", "Migration")]
public class MigrationTests
{
    private readonly OrdersApiFactory _factory;

    public MigrationTests(OrdersApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AllMigrations_Apply_Successfully()
    {
        using var scope  = _factory.Services.CreateScope();
        var dbContext    = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        // La factory usa EnsureCreatedAsync (InitializeAsync) — las tablas deben existir
        var tables = await dbContext.Database
            .SqlQueryRaw<string>(
                "SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'orders'")
            .ToListAsync();

        tables.Should().Contain("Orders");
        tables.Should().Contain("OrderLines");
        tables.Should().Contain("OrderSummaries");
        tables.Should().Contain("AuditLogs");
    }

    [Fact]
    public async Task NoPendingMigrations_InCurrentBranch()
    {
        // Si alguien modifica OrderDbContext sin crear la migration correspondiente,
        // este test lo detecta. Usa la BD real (OrderFlowOrders) donde aplicamos migrations.
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=OrderFlowOrders;" +
                "Trusted_Connection=true;TrustServerCertificate=true",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders"))
            .Options;

        await using var ctx = new OrderDbContext(options);
        var pending = await ctx.Database.GetPendingMigrationsAsync();

        pending.Should().BeEmpty(
            "todo cambio de modelo requiere su migration correspondiente");
    }

    [Fact]
    public async Task ExpandMigrations_AreAdditiveOnly()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=OrderFlowOrders;" +
                "Trusted_Connection=true;TrustServerCertificate=true",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders"))
            .Options;

        await using var ctx = new OrderDbContext(options);
        var applied = await ctx.Database.GetAppliedMigrationsAsync();

        var expandMigrations = applied
            .Where(m => m.Contains("Expand", StringComparison.OrdinalIgnoreCase))
            .ToList();

        expandMigrations.Should().NotBeEmpty(
            "al menos una migration 'Expand' debe existir (m6.4)");
    }

    [Fact]
    public async Task DiscountAmountColumn_Exists_And_IsNotNull()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=OrderFlowOrders;" +
                "Trusted_Connection=true;TrustServerCertificate=true",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders"))
            .Options;

        await using var ctx = new OrderDbContext(options);

        var isNullable = await ctx.Database.SqlQueryRaw<string>(
            "SELECT IS_NULLABLE AS Value FROM INFORMATION_SCHEMA.COLUMNS " +
            "WHERE TABLE_SCHEMA = 'orders' AND TABLE_NAME = 'Orders' AND COLUMN_NAME = 'DiscountAmount'")
            .FirstOrDefaultAsync();

        isNullable.Should().Be("NO", "tras Fase 3 (Contract) debe ser NOT NULL");
    }

    [Fact]
    public async Task BuyerIdColumn_Exists_And_MatchesCustomerId()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=OrderFlowOrders;" +
                "Trusted_Connection=true;TrustServerCertificate=true",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders"))
            .Options;

        await using var ctx = new OrderDbContext(options);

        var exists = await ctx.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS " +
            "WHERE TABLE_SCHEMA = 'orders' AND TABLE_NAME = 'Orders' AND COLUMN_NAME = 'BuyerId'")
            .FirstOrDefaultAsync();

        exists.Should().Be(1, "la migration ExpandAddBuyerIdColumn debe haber añadido BuyerId");
    }
}
