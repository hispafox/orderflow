using System.Diagnostics;

namespace Orders.API.Infrastructure.Telemetry;

public static class OrdersTelemetry
{
    public static readonly ActivitySource ActivitySource =
        new ActivitySource("Orders.API", "1.0.0");
}
