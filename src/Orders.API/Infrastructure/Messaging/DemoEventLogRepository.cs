using Dapper;
using Microsoft.Data.SqlClient;

namespace Orders.API.Infrastructure.Messaging;

public interface IDemoEventLogRepository
{
    Task InsertAsync(DemoEventLog row, CancellationToken ct = default);
    Task<IReadOnlyList<DemoEventLog>> GetRecentAsync(
        Guid? correlationId, int limit, CancellationToken ct = default);
}

public sealed class DemoEventLogRepository : IDemoEventLogRepository
{
    private readonly string _connectionString;

    public DemoEventLogRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("sqlserver")
            ?? throw new InvalidOperationException("Connection string 'sqlserver' no definida");
    }

    public async Task InsertAsync(DemoEventLog row, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO [orders].[DemoEventLog]
                (OccurredAt, Direction, MessageType, DestinationAddress, SourceAddress,
                 MessageId, CorrelationId, ConversationId, ServiceName)
            VALUES
                (@OccurredAt, @Direction, @MessageType, @DestinationAddress, @SourceAddress,
                 @MessageId, @CorrelationId, @ConversationId, @ServiceName);";

        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, row, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DemoEventLog>> GetRecentAsync(
        Guid? correlationId, int limit, CancellationToken ct = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);

        var sql = correlationId is null
            ? $@"SELECT TOP ({safeLimit}) Id, OccurredAt, Direction, MessageType, DestinationAddress,
                                   SourceAddress, MessageId, CorrelationId, ConversationId, ServiceName
                 FROM [orders].[DemoEventLog]
                 ORDER BY Id DESC"
            : $@"SELECT TOP ({safeLimit}) Id, OccurredAt, Direction, MessageType, DestinationAddress,
                                   SourceAddress, MessageId, CorrelationId, ConversationId, ServiceName
                 FROM [orders].[DemoEventLog]
                 WHERE CorrelationId = @CorrelationId OR ConversationId = @CorrelationId
                 ORDER BY Id DESC";

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<DemoEventLog>(
            new CommandDefinition(sql, new { CorrelationId = correlationId }, cancellationToken: ct));
        return rows.ToList();
    }
}
