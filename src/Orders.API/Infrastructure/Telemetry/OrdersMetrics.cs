using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Orders.API.Infrastructure.Telemetry;

public class OrdersMetrics
{
    private readonly Counter<long> _ordersCreated;
    private readonly Counter<long> _ordersFailed;
    private readonly Histogram<double> _processingTime;

    public OrdersMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Orders.API");

        _ordersCreated = meter.CreateCounter<long>(
            "orders.created.total",
            description: "Total de pedidos creados correctamente");

        _ordersFailed = meter.CreateCounter<long>(
            "orders.failed.total",
            description: "Total de pedidos que fallaron durante su creación");

        _processingTime = meter.CreateHistogram<double>(
            "orders.processing.duration.ms",
            unit: "ms",
            description: "Duración del procesamiento de un pedido en milisegundos");
    }

    public void RecordOrderCreated(string currency, string customerType) =>
        _ordersCreated.Add(1, new TagList
        {
            { "currency", currency },
            { "customer_type", customerType }
        });

    public void RecordOrderFailed(string reason) =>
        _ordersFailed.Add(1, new TagList { { "reason", reason } });

    public void RecordProcessingTime(double milliseconds) =>
        _processingTime.Record(milliseconds);
}
