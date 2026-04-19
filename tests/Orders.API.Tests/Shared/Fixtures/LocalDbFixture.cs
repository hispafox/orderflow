namespace Orders.API.Tests.Shared.Fixtures;

// TODO: M3.2 — implementar con OrderDbContext cuando EF Core esté configurado
// Crea una BD con nombre único en LocalDB, aplica migrations y la elimina al finalizar.
// Usar con [Collection("SqlServerCollection")] para tests de repositorio.
public class LocalDbFixture : IAsyncLifetime
{
    private readonly string _testDbName = $"OrdersTestDb_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;" +
        $"Database={_testDbName};" +
        $"Trusted_Connection=true;" +
        $"TrustServerCertificate=true;";

    public Task InitializeAsync() => Task.CompletedTask; // M3.2: aplicar migrations

    public Task DisposeAsync() => Task.CompletedTask;    // M3.2: eliminar BD
}

[CollectionDefinition("SqlServerCollection")]
public class SqlServerCollection : ICollectionFixture<LocalDbFixture> { }
