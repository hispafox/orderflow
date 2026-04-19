var builder = WebApplication.CreateBuilder(args);

// Una sola línea: OpenTelemetry + health checks + service discovery
builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // expone /openapi/v1.json
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Expone /health y /alive — registrados por ServiceDefaults
app.MapDefaultEndpoints();

app.Run();
