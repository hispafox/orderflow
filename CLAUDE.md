# CLAUDE.md — OrderFlow Microservices

> Este fichero va en la raíz del repo `orderflow/`.
> Claude Code lo lee automáticamente al arrancar cada sesión.
> Contiene todo lo que necesitas saber para trabajar en este proyecto.

---

## Qué es este proyecto

**OrderFlow** es el sistema de microservicios de **TechShop**, una empresa ficticia de e-commerce usada como hilo conductor del curso *"Microservicios con .NET 10"* (25 horas).

5 servicios independientes:

| Servicio | Responsabilidad | Puerto local |
|---|---|---|
| `Orders.API` | Gestión de pedidos, Saga orchestrator | 5100 / 7100 |
| `Products.API` | Catálogo de productos, reserva de stock | 5200 / 7200 |
| `Payments.API` | Procesamiento de pagos | 5300 / 7300 |
| `Notifications.API` | Envío de emails y notificaciones | 5400 / 7400 |
| `Gateway.API` | YARP Reverse Proxy — punto de entrada único | 5000 / 7000 |

---

## Stack tecnológico — versiones exactas

| Tecnología | Versión | Uso |
|---|---|---|
| .NET | 10 LTS | Runtime y SDK |
| C# | 14 | Lenguaje |
| ASP.NET Core | 10 | Framework web |
| .NET Aspire | 13.x | Orquestación local + Dashboard |
| Entity Framework Core | 10 | ORM (write side) |
| Dapper | 2.x | Read model (queries) |
| MediatR | 12.x | CQRS + Pipeline Behaviors |
| FluentValidation | 11.x | Validación de commands/requests |
| MassTransit | 8.x | Mensajería asíncrona |
| Polly | 8.x | Resiliencia (Retry, Circuit Breaker, Timeout) |
| YARP | 2.x | Reverse Proxy para Gateway |
| Serilog | 4.x | Structured logging |
| OpenTelemetry | 1.10.x | Traces + Metrics |
| ASP.NET Core Identity | 10 | Autenticación local → Azure AD en producción |
| xUnit | 2.x | Framework de tests |
| FluentAssertions | 8.x | Assertions en tests |
| NSubstitute | 5.x | Mocking en tests |
| Microsoft.AspNetCore.Mvc.Testing | 10 | WebApplicationFactory (integration tests) |

---

## Estructura de carpetas de la solución

```
orderflow/
├── CLAUDE.md                          ← este fichero
├── OrderFlow.slnx
├── .gitignore
├── .editorconfig
│
├── docs/                              ← guías de implementación por módulo
│   ├── setup.md                       ← ejecutar UNA vez para crear la solución
│   ├── m2.1.md
│   ├── m2.2.md
│   └── ...
│
├── infrastructure/
│   ├── OrderFlow.AppHost/             ← orquestador Aspire
│   │   ├── OrderFlow.AppHost.csproj
│   │   └── Program.cs
│   └── OrderFlow.ServiceDefaults/     ← configuración compartida (OTel, HC, SD)
│       ├── OrderFlow.ServiceDefaults.csproj
│       └── Extensions.cs
│
├── shared/
│   └── OrderFlow.Contracts/           ← eventos de integración (IIntegrationEvent)
│       ├── OrderFlow.Contracts.csproj
│       └── Events/
│           ├── Orders/
│           │   ├── OrderCreatedEvent.cs
│           │   ├── OrderConfirmedEvent.cs
│           │   └── OrderCancelledEvent.cs
│           ├── Products/
│           │   └── StockReservedEvent.cs
│           └── Payments/
│               ├── PaymentProcessedEvent.cs
│               └── PaymentFailedEvent.cs
│
├── src/
│   ├── Orders.API/
│   │   ├── Orders.API.csproj
│   │   ├── Program.cs
│   │   ├── Properties/launchSettings.json
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Controllers/               ← Controllers MVC
│   │   ├── Application/               ← CQRS: Commands, Queries, Handlers, Behaviors
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── Behaviors/
│   │   ├── Domain/                    ← Entidades, Value Objects, Domain Events
│   │   │   ├── Entities/
│   │   │   ├── ValueObjects/
│   │   │   └── Events/
│   │   └── Infrastructure/            ← DbContext, Repositories, Outbox
│   │       ├── Persistence/
│   │       └── Messaging/
│   │
│   ├── Products.API/                  ← Minimal APIs (a partir de M3.3)
│   ├── Payments.API/                  ← Controllers (a partir de M4.3)
│   ├── Notifications.API/             ← Workers + MassTransit (a partir de M4.2)
│   └── Gateway.API/                   ← YARP (a partir de M4.4)
│
└── tests/
    ├── Orders.API.Tests/
    │   ├── Orders.API.Tests.csproj
    │   ├── Unit/
    │   │   ├── Domain/
    │   │   ├── Handlers/
    │   │   └── Validators/
    │   ├── Integration/
    │   │   ├── Endpoints/
    │   │   └── Repositories/
    │   └── Helpers/
    │       ├── LocalDbFixture.cs      ← fixture compartida para tests con BD
    │       └── OrdersWebApplicationFactory.cs
    ├── Products.API.Tests/
    ├── Payments.API.Tests/
    └── Gateway.API.Tests/
```

---

## Arquitectura — Clean Architecture

Cada microservicio sigue **Clean Architecture** (no Hexagonal, no Onion — se llama Clean Architecture).

```
┌─────────────────────────────────────────┐
│           Controllers / Endpoints        │  ← Capa de Presentación
├─────────────────────────────────────────┤
│     Commands │ Queries │ Handlers        │  ← Capa de Aplicación
│     Behaviors │ DTOs │ Validators        │
├─────────────────────────────────────────┤
│     Entities │ Value Objects             │  ← Capa de Dominio
│     Domain Events │ Interfaces           │
├─────────────────────────────────────────┤
│     DbContext │ Repositories             │  ← Capa de Infraestructura
│     MassTransit │ Outbox                 │
└─────────────────────────────────────────┘
```

**La Dependency Rule es sagrada:** las capas internas no conocen a las externas. El Dominio no tiene referencias a EF Core ni a MassTransit. La Aplicación no tiene referencias a Controllers.

---

## Bases de datos — Database per Service

Cada servicio tiene su propia base de datos. No hay tablas compartidas entre servicios.

| Servicio | Base de datos | Schema principal |
|---|---|---|
| Orders.API | `OrderFlowOrders` | `orders` |
| Products.API | `OrderFlowProducts` | `products` |
| Payments.API | `OrderFlowPayments` | `payments` |
| Notifications.API | `OrderFlowNotifications` | `notifications` |

**En desarrollo:** SQL Server LocalDB — `(localdb)\MSSQLLocalDB`
**En producción:** Azure SQL (M7.2)

Las migraciones de EF Core se aplican en startup con `MigrationBundles` (M6.1). Nunca se usa `Database.EnsureCreated()` en producción.

---

## Infraestructura local — SIN Docker

Este proyecto NO usa Docker. Los servicios externos se instalan nativamente en Windows:

| Servicio | Instalación | Connection string |
|---|---|---|
| SQL Server | SQL Server LocalDB (con Visual Studio) | `Server=(localdb)\MSSQLLocalDB;Database=OrderFlow;Trusted_Connection=true;TrustServerCertificate=true` |
| RabbitMQ | Instalación nativa Windows (puerto 5672) | `amqp://guest:guest@localhost:5672` |

En el AppHost, los recursos externos se registran con `AddConnectionString`:

```csharp
// ✅ CORRECTO — sin Docker
var sqlServer = builder.AddConnectionString("sqlserver");
var messaging = builder.AddConnectionString("messaging");

// ❌ INCORRECTO — esto levanta contenedores Docker
// var sqlServer = builder.AddSqlServer("sqlserver");
// var messaging = builder.AddRabbitMQ("messaging");
```

---

## Aspire — orquestador local

.NET Aspire 13.x orquesta todos los servicios en desarrollo. El AppHost es el proyecto de entrada.

```bash
# Arrancar todos los servicios
dotnet run --project infrastructure/OrderFlow.AppHost
```

El Dashboard de Aspire muestra en tiempo real:
- Estado de cada servicio (Running, Unhealthy, Starting)
- Logs estructurados de todos los servicios
- Trazas distribuidas (OpenTelemetry)
- Métricas de rendimiento

**Service Discovery:** los servicios se llaman por nombre lógico, no por puerto:

```csharp
// ✅ CORRECTO — Aspire resuelve el nombre a la URL real
httpClient.BaseAddress = new Uri("http://products-api");

// ❌ INCORRECTO — hardcoded
httpClient.BaseAddress = new Uri("http://localhost:5200");
```

---

## Convenciones de código

### Nombres y namespaces

Los namespaces siguen la estructura de carpetas:

```csharp
// ✅ CORRECTO
namespace Orders.API.Application.Commands;
namespace Orders.API.Domain.Entities;
namespace Orders.API.Infrastructure.Persistence;

// ❌ INCORRECTO
namespace OrdersAPI.Commands;
namespace TechShop.Orders;
```

### Clases, records y sealed

```csharp
// Commands y Queries — siempre records sealed
public sealed record CreateOrderCommand(Guid CustomerId, List<OrderItemDto> Items) : IRequest<Guid>;

// Responses/DTOs — records sealed
public sealed record OrderDto(Guid Id, Guid CustomerId, OrderStatus Status, decimal Total);

// Entidades — clases (necesitan mutabilidad controlada)
public class Order { }

// Value Objects — records sealed
public sealed record Money(decimal Amount, string Currency);
```

### Async/await

```csharp
// ✅ CORRECTO — siempre await, nunca .Result ni .Wait()
var order = await _repository.GetByIdAsync(id, cancellationToken);

// ❌ INCORRECTO
var order = _repository.GetByIdAsync(id).Result;
var order = _repository.GetByIdAsync(id).GetAwaiter().GetResult();
```

### CancellationToken

```csharp
// ✅ CORRECTO — CancellationToken en todos los métodos async
public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)

// ❌ INCORRECTO — ignorar el CancellationToken
public async Task<Order?> GetByIdAsync(Guid id)
```

### Nullable Reference Types

Todos los proyectos tienen `<Nullable>enable</Nullable>`. Nunca usar `#pragma warning disable nullable`.

```csharp
// ✅ CORRECTO — explicitar cuando puede ser null
public Order? GetById(Guid id) { ... }

// ✅ CORRECTO — null-check explícito
if (order is null) return NotFound();

// ❌ INCORRECTO — suprimir el warning
#pragma warning disable CS8603
return order;
```

### Logging estructurado

```csharp
// ✅ CORRECTO — structured logging con placeholders
_logger.LogInformation("Creating order for customer {CustomerId} with {ItemCount} items",
    command.CustomerId, command.Items.Count);

// ❌ INCORRECTO — string interpolation en logging
_logger.LogInformation($"Creating order for customer {command.CustomerId}");
```

---

## Program.cs — estructura obligatoria

Todo `Program.cs` de un microservicio sigue este orden:

```csharp
using ...;

var builder = WebApplication.CreateBuilder(args);

// 1. Aspire ServiceDefaults (siempre primero)
builder.AddServiceDefaults();

// 2. Configuración (Options Pattern)
builder.Services.AddOptions<OrdersSettings>()
    .BindConfiguration("Orders")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 3. Infraestructura (DbContext, Repos, etc.)
builder.Services.AddDbContext<OrdersDbContext>(...);

// 4. Aplicación (MediatR, Validators, etc.)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// 5. API (Controllers u OpenAPI)
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 6. Pipeline
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints(); // /health y /alive — SIEMPRE al final

app.Run();

public partial class Program { } // ← necesario para WebApplicationFactory
```

---

## CQRS con MediatR — patrón obligatorio para Orders.API

A partir de M3.4, todos los endpoints pasan por MediatR:

```
Controller → IMediator.Send(Command/Query) → Handler → Repository/DbContext
```

Pipeline Behaviors en orden:
1. `LoggingBehavior` — traza entrada/salida con Serilog
2. `ValidationBehavior` — ejecuta FluentValidation, lanza `ValidationException`
3. `PerformanceBehavior` — log de warning si supera 500ms

---

## Tests — reglas obligatorias

### Stack de tests

```
xUnit              → framework de tests
FluentAssertions   → assertions (Never use Assert.Equal)
NSubstitute        → mocking (NUNCA Moq)
WebApplicationFactory → integration tests (NUNCA TestServer manual)
LocalDbFixture     → tests con base de datos (NUNCA InMemoryDatabase)
```

### Nomenclatura de tests

```csharp
// Patrón: Método_Escenario_ResultadoEsperado
[Fact]
public async Task Handle_WhenCustomerIdIsEmpty_ThrowsValidationException() { }

[Fact]
public async Task GetById_WhenOrderDoesNotExist_Returns404() { }
```

### Estructura de tests de integración

```csharp
public class OrdersEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrdersEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturn200WithEmptyArray() { }
}
```

### Tests con base de datos — LocalDbFixture

A partir de M2.4, los tests de repositorio usan `LocalDbFixture`:

```csharp
public class OrdersRepositoryTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public OrdersRepositoryTests(LocalDbFixture fixture)
    {
        _fixture = fixture;
    }
}
```

`LocalDbFixture` crea una base de datos de test con nombre único en LocalDB, aplica las migraciones, y la elimina al finalizar la suite.

### CI: windows-latest obligatorio para tests con BD

```yaml
# ✅ CORRECTO — LocalDB solo existe en Windows
runs-on: windows-latest

# ❌ INCORRECTO — LocalDB no está disponible en Linux
runs-on: ubuntu-latest
```

---

## Mensajería — MassTransit + RabbitMQ → Azure Service Bus

En local usamos RabbitMQ (puerto 5672). En producción (M7.x) se cambia a Azure Service Bus con **una sola línea**.

```csharp
// Local (Development)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(ctx);
    });
});

// Producción (reemplaza el bloque UsingRabbitMq)
builder.Services.AddMassTransit(x =>
{
    x.UsingAzureServiceBus((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("servicebus"));
        cfg.ConfigureEndpoints(ctx);
    });
});
```

Los eventos de integración se definen en `shared/OrderFlow.Contracts/Events/` y se publican con `IPublishEndpoint`.

---

## Seguridad

**En desarrollo (hasta M5.x):** sin autenticación — los endpoints son públicos.

**A partir de M5.1:** ASP.NET Core Identity + JWT Bearer tokens.

**NUNCA Keycloak** — se usa ASP.NET Core Identity para simplificar la infraestructura local. En producción se integra con Azure AD.

### Secretos

```bash
# ✅ CORRECTO — User Secrets para desarrollo local
dotnet user-secrets set "ConnectionStrings:sqlserver" "Server=(localdb)\\MSSQLLocalDB;..."

# ❌ INCORRECTO — credenciales en appsettings.json
{
  "ConnectionStrings": {
    "sqlserver": "...password=admin123..."
  }
}
```

---

## Ramas del repositorio

Las ramas son **secuenciales**: cada una parte de la anterior.

```
main (setup inicial, sin código de negocio)
  └── m2.1  (Orders.API skeleton + Aspire)
        └── m2.2  (Options Pattern + Health Checks + User Secrets)
              └── m2.3  (Serilog + OpenTelemetry + CorrelationId)
                    └── m2.4  (Tests: LocalDbFixture + Integration Tests)
                          └── m3.1  (Clean Architecture + Domain Layer)
                                └── m3.2  (EF Core + Repository Pattern)
                                      └── m3.3  (Products.API con Minimal APIs)
                                            └── m3.4  (CQRS con MediatR)
                                                  └── m3.5  (Polly + Resiliencia)
                                                        └── m3.6  (Lab: consolidación)
                                                              └── m4.1  (HttpClient + Refit)
                                                                    └── m4.2  (RabbitMQ + Notifications.API)
                                                                          └── m4.3  (Payments.API + Saga)
                                                                                └── m4.4  (YARP Gateway)
                                                                                      └── m5.1  (Identity + JWT)
                                                                                            └── m5.2  (Authorization Policies)
                                                                                                  └── m5.3  (Rate Limiting + Security)
                                                                                                        └── m6.1  (Migrations + EnsureCreated)
                                                                                                              └── m6.2  (Outbox Pattern)
                                                                                                                    └── m6.3  (Read Model + Dapper)
                                                                                                                          └── m6.4  (Performance + Caching)
                                                                                                                                └── m7.1  (GitHub Actions CI)
                                                                                                                                      └── m7.2  (Azure App Services)
                                                                                                                                            └── m7.3  (CD + Blue-Green)
                                                                                                                                                  └── m7.4  (Application Insights)
```

---

## Workflow para implementar un módulo

Cuando te digan "implementa el módulo M_X_Y", el flujo es:

```
1. Lee docs/mX.Y.md — contiene TODO lo que hay que hacer
2. Confirma en qué rama estás con: git branch --show-current
3. Si la rama no existe: git checkout <rama-anterior> && git checkout -b mX.Y
4. Ejecuta todos los pasos del fichero docs/mX.Y.md en orden
5. Compila: dotnet build OrderFlow.slnx (0 errores, 0 warnings)
6. Tests: dotnet test OrderFlow.slnx (todos deben pasar)
7. Arranca Aspire y verifica manualmente que todo funciona
8. Commit con el mensaje exacto del fichero docs/mX.Y.md
9. Push: git push origin mX.Y
```

**Nunca hacer commit si los tests fallan.**
**Nunca saltar pasos de verificación.**

---

## LO QUE NUNCA DEBES HACER

Estas reglas son **inquebrantables**. Si algo contradice estas reglas, la regla prevalece.

1. **No usar Docker** — usar LocalDB y RabbitMQ instalados nativamente
2. **No usar `builder.AddSqlServer()` ni `builder.AddRabbitMQ()`** — usar `AddConnectionString()`
3. **No usar `InMemoryDatabase`** en tests — usar LocalDB real con `LocalDbFixture`
4. **No usar TestContainers** — usar `LocalDbFixture`
5. **No usar Keycloak** — usar ASP.NET Core Identity
6. **No usar `ubuntu-latest`** en jobs de GitHub Actions con base de datos — usar `windows-latest`
7. **No usar `.Result` ni `.Wait()`** en código async — usar `await`
8. **No ignorar `CancellationToken`** — propagarlo siempre
9. **No poner secretos en `appsettings.json`** — usar User Secrets
10. **No llamar a la arquitectura "hexagonal"** — es Clean Architecture
11. **No crear un DbContext compartido** — cada servicio tiene el suyo
12. **No usar `Database.EnsureCreated()`** en producción — usar Migration Bundles
13. **No usar Moq** — usar NSubstitute

---

## Verificación rápida antes de cada commit

```bash
# Compila todo
dotnet build OrderFlow.slnx

# Tests pasan (0 failing)
dotnet test OrderFlow.slnx --verbosity normal

# Sin warnings de nullable
dotnet build OrderFlow.slnx -warnaserror

# Sin secretos commiteados
git diff --cached -- "*.json" | grep -i "password\|secret\|apikey\|connectionstring"
```
