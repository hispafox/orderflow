using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.API.API.DTOs;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Interfaces;
using Orders.API.Application.Queries;

namespace Orders.API.Infrastructure.Persistence.ReadModel;

public class OrderReadRepository : IOrderReadRepository
{
    private readonly string _connectionString;
    private readonly ILogger<OrderReadRepository> _logger;

    private static readonly Meter _meter = new("Orders.API.ReadModel");
    private static readonly Histogram<double> _queryDuration =
        _meter.CreateHistogram<double>(
            "orders.read_model.query.duration.ms",
            unit:        "ms",
            description: "Duration of Read Model Dapper queries");

    public OrderReadRepository(
        IConfiguration               config,
        ILogger<OrderReadRepository> logger)
    {
        _connectionString = config.GetConnectionString("sqlserver")!;
        _logger           = logger;
    }

    public async Task<OrderDto?> GetByIdAsync(
        Guid orderId, Guid requestingUserId, bool isAdmin,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(_connectionString);

            const string sql = """
                SELECT
                    o.Id, o.CustomerId,
                    o.Status,
                    o.TotalAmount AS Total,
                    o.TotalCurrency AS Currency,
                    o.CreatedAt, o.ConfirmedAt, o.CancelledAt, o.CancellationReason,
                    o.ShippingStreet  AS Street,
                    o.ShippingCity    AS City,
                    o.ShippingZipCode AS ZipCode,
                    o.ShippingCountry AS Country,
                    l.Id AS LineId,
                    l.Id, l.ProductId, l.ProductName, l.Quantity,
                    l.UnitPriceAmount AS UnitPrice,
                    (l.Quantity * l.UnitPriceAmount) AS LineTotal
                FROM   orders.Orders o
                LEFT JOIN orders.OrderLines l ON l.OrderId = o.Id
                WHERE  o.Id = @OrderId
                  AND  o.IsDeleted = 0
                  AND  (@IsAdmin = 1 OR o.CustomerId = @UserId)
                """;

            OrderDto? result = null;
            var lines = new List<OrderLineDto>();
            var address = new OrderAddressDto();

            await connection.QueryAsync<OrderRow, OrderLineDto, OrderDto>(
                sql,
                (row, line) =>
                {
                    if (result is null)
                    {
                        address = new OrderAddressDto
                        {
                            Street  = row.Street  ?? string.Empty,
                            City    = row.City    ?? string.Empty,
                            ZipCode = row.ZipCode ?? string.Empty,
                            Country = row.Country ?? string.Empty
                        };
                        result = new OrderDto
                        {
                            Id                 = row.Id,
                            CustomerId         = row.CustomerId,
                            Status             = row.Status,
                            Total              = row.Total,
                            Currency           = row.Currency,
                            CreatedAt          = row.CreatedAt,
                            ConfirmedAt        = row.ConfirmedAt,
                            CancelledAt        = row.CancelledAt,
                            CancellationReason = row.CancellationReason,
                            ShippingAddress    = address,
                            Lines              = lines
                        };
                    }
                    if (line?.Id != Guid.Empty) lines.Add(line!);
                    return result;
                },
                splitOn: "LineId",
                param:   new { OrderId = orderId, UserId = requestingUserId, IsAdmin = isAdmin ? 1 : 0 });

            return result;
        }
        finally
        {
            sw.Stop();
            _queryDuration.Record(sw.Elapsed.TotalMilliseconds,
                new TagList { { "query", "get_by_id" } });
        }
    }

    public async Task<PagedResult<OrderSummaryDto>> ListAsync(
        Guid? customerId, string? status,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(_connectionString);

            var where      = new List<string>();
            var parameters = new DynamicParameters();

            if (customerId.HasValue)
            {
                where.Add("CustomerId = @CustomerId");
                parameters.Add("CustomerId", customerId.Value);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                where.Add("Status = @Status");
                parameters.Add("Status", status);
            }

            var whereClause = where.Any() ? $"WHERE {string.Join(" AND ", where)}" : string.Empty;
            var skip = (page - 1) * pageSize;

            var countSql = $"SELECT COUNT(1) FROM orders.OrderSummaries {whereClause}";
            var dataSql  = $"""
                SELECT OrderId AS Id, CustomerId, CustomerEmail, Status,
                       TotalAmount AS Total, Currency,
                       LinesCount AS LineCount, FirstItemName,
                       CreatedAt, ConfirmedAt, ShippingCity
                FROM   orders.OrderSummaries {whereClause}
                ORDER  BY CreatedAt DESC
                OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY
                """;

            var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var items = await connection.QueryAsync<OrderSummaryDto>(dataSql, parameters);

            return new PagedResult<OrderSummaryDto>
            {
                Items      = items.ToList(),
                TotalCount = total,
                Page       = page,
                PageSize   = pageSize
            };
        }
        finally
        {
            sw.Stop();
            _queryDuration.Record(sw.Elapsed.TotalMilliseconds,
                new TagList { { "query", "list_orders" } });
        }
    }

    public async Task<PagedResult<OrderSummaryDto>> SearchAsync(
        OrderSearchQuery query, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var where      = new List<string> { "1=1" };
            var parameters = new DynamicParameters();

            if (query.CustomerId.HasValue)
            {
                where.Add("CustomerId = @CustomerId");
                parameters.Add("CustomerId", query.CustomerId.Value);
            }
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                where.Add("Status = @Status");
                parameters.Add("Status", query.Status);
            }
            if (query.From.HasValue)
            {
                where.Add("CreatedAt >= @From");
                parameters.Add("From", query.From.Value);
            }
            if (query.To.HasValue)
            {
                where.Add("CreatedAt <= @To");
                parameters.Add("To", query.To.Value);
            }
            if (!string.IsNullOrWhiteSpace(query.ShippingCity))
            {
                where.Add("ShippingCity = @City");
                parameters.Add("City", query.ShippingCity);
            }
            if (query.MinTotal.HasValue)
            {
                where.Add("TotalAmount >= @MinTotal");
                parameters.Add("MinTotal", query.MinTotal.Value);
            }

            var whereClause = $"WHERE {string.Join(" AND ", where)}";
            var skip        = (query.Page - 1) * query.PageSize;

            var countSql = $"SELECT COUNT(1) FROM orders.OrderSummaries {whereClause}";
            var dataSql  = $"""
                SELECT OrderId AS Id, CustomerId, CustomerEmail, Status,
                       TotalAmount AS Total, Currency,
                       LinesCount AS LineCount, FirstItemName,
                       CreatedAt, ConfirmedAt, ShippingCity
                FROM   orders.OrderSummaries {whereClause}
                ORDER  BY CreatedAt DESC
                OFFSET {skip} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
                """;

            var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var items = await connection.QueryAsync<OrderSummaryDto>(dataSql, parameters);

            return new PagedResult<OrderSummaryDto>
            {
                Items      = items.ToList(),
                TotalCount = total,
                Page       = query.Page,
                PageSize   = query.PageSize
            };
        }
        finally
        {
            sw.Stop();
            _queryDuration.Record(sw.Elapsed.TotalMilliseconds,
                new TagList { { "query", "search_orders" } });
        }
    }

    public async Task<OrderStatsDto> GetStatsByCustomerAsync(
        Guid customerId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(_connectionString);

            const string sql = """
                SELECT
                    COUNT(*)                                             AS TotalOrders,
                    SUM(CASE WHEN Status = 'Pending'   THEN 1 ELSE 0 END) AS PendingOrders,
                    SUM(CASE WHEN Status = 'Confirmed' THEN 1 ELSE 0 END) AS ConfirmedOrders,
                    ISNULL(SUM(TotalAmount), 0)                          AS TotalSpent,
                    ISNULL(MAX(TotalAmount), 0)                          AS LargestOrder,
                    MIN(CreatedAt)                                       AS FirstOrderDate,
                    MAX(CreatedAt)                                       AS LastOrderDate
                FROM orders.OrderSummaries
                WHERE CustomerId = @CustomerId
                """;

            return await connection.QuerySingleAsync<OrderStatsDto>(
                sql, new { CustomerId = customerId });
        }
        finally
        {
            sw.Stop();
            _queryDuration.Record(sw.Elapsed.TotalMilliseconds,
                new TagList { { "query", "stats_by_customer" } });
        }
    }

    private record OrderRow(
        Guid      Id,
        Guid      CustomerId,
        string    Status,
        decimal   Total,
        string    Currency,
        DateTime  CreatedAt,
        DateTime? ConfirmedAt,
        DateTime? CancelledAt,
        string?   CancellationReason,
        string?   Street,
        string?   City,
        string?   ZipCode,
        string?   Country);
}
