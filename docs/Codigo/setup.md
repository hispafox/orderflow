# Setup — Ramas y Estructura Inicial

## Estrategia de ramas

Cada módulo vive en su propia rama. Al terminar, se mergea a `main`. Así `main` siempre tiene el estado acumulado más reciente del curso.

```
start ──────────────────────────────────────────► main
         m2.1 ──────────────────────────────────► main
                  m2.2 ──────────────────────────► main
                           m2.3 ────────────────► main
                                    m2.4 ────────► main
                                             ...► main
```

- **`start`** — skeleton vacío: solución + AppHost + ServiceDefaults. Sin código de negocio.
- **`mX.Y`** — cada rama parte de la anterior (`m2.2` parte de `m2.1`, etc.)
- **`main`** — recibe el merge de cada módulo. Es el proyecto completo hasta el punto actual del curso.

> Los alumnos pueden hacer `git checkout m2.3` para ver el estado exacto al final de ese módulo,
> o quedarse en `main` para tener siempre lo último.

---

## Paso 0 — Crear la rama `start`

Ejecutar **una sola vez** desde la raíz del repositorio.

### 0.1 Crear la solución

```bash
git checkout -b start

dotnet new sln -n OrderFlow
```

### 0.2 Crear OrderFlow.ServiceDefaults

En .NET 10 no existen las templates `aspire-servicedefaults`/`aspire-apphost` — Aspire se usa como NuGet packages directamente.

Crear `infrastructure/OrderFlow.ServiceDefaults/OrderFlow.ServiceDefaults.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.*" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="10.*" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.*" />
  </ItemGroup>

</Project>
```

Copiar `Extensions.cs` del repo (ya existe en `infrastructure/OrderFlow.ServiceDefaults/Extensions.cs`).

```bash
dotnet sln OrderFlow.slnx add \
  infrastructure/OrderFlow.ServiceDefaults/OrderFlow.ServiceDefaults.csproj
```

### 0.3 Crear OrderFlow.AppHost

Crear `infrastructure/OrderFlow.AppHost/OrderFlow.AppHost.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Sdk Name="Aspire.AppHost.Sdk" Version="13.2.2" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsAspireHost>true</IsAspireHost>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="13.*" />
  </ItemGroup>

</Project>
```

> **Nota:** El `<Sdk Name="Aspire.AppHost.Sdk">` requiere versión exacta (no wildcards). Usar la última disponible en NuGet.

```bash
dotnet sln OrderFlow.slnx add \
  infrastructure/OrderFlow.AppHost/OrderFlow.AppHost.csproj
```

### 0.4 Contenido de AppHost/Program.cs

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Recursos externos — sin Docker
// Las connection strings se configuran en appsettings.json o User Secrets
var sqlServer = builder.AddConnectionString("sqlserver");
var messaging = builder.AddConnectionString("messaging");

// Los servicios se añaden módulo a módulo:
// M2.1 → orders-api
// M3.3 → products-api
// M4.2 → notifications-api
// M4.3 → payments-api
// M4.4 → gateway-api

builder.Build().Run();
```

### 0.5 Reemplazar AppHost/appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Aspire.Hosting": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "sqlserver": "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=true;TrustServerCertificate=true;",
    "messaging": "amqp://guest:guest@localhost:5672"
  }
}
```

> **Nota:** Las connection strings van aquí en desarrollo. En producción se usan variables de entorno o Azure Key Vault. Nunca poner credenciales reales en este fichero.

### 0.6 Crear .editorconfig

Crear `.editorconfig` en la raíz del repositorio:

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
insert_final_newline = true
charset = utf-8
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_prefer_braces = true:silent
csharp_prefer_simple_using_statement = true:suggestion

[*.{json,csproj,props,targets}]
indent_style = space
indent_size = 2
insert_final_newline = true
charset = utf-8

[*.{yml,yaml}]
indent_style = space
indent_size = 2
insert_final_newline = true
```

### 0.7 Verificar que compila

```bash
dotnet build OrderFlow.slnx
```

Sin errores. Sin warnings.

### 0.8 Commit y merge a main

```bash
git add .
git commit -m "start: solution skeleton

- OrderFlow.slnx
- infrastructure/OrderFlow.ServiceDefaults
- infrastructure/OrderFlow.AppHost (sin servicios registrados)
- .editorconfig"

git push origin start

# Mergear a main
git checkout main
git merge start --no-ff -m "merge: start → main"
git push origin main
```

---

## Workflow por módulo

Repetir este proceso para cada módulo (`m2.1`, `m2.2`, `m2.3`, ...):

```bash
# 1. Partir de la rama anterior
git checkout m2.1          # o la rama del módulo anterior
git checkout -b m2.2       # crear la nueva rama

# 2. Implementar según docs/Codigo/mX.Y.md
#    (leer el fichero completo antes de tocar nada)

# 3. Verificar
dotnet build OrderFlow.slnx                    # 0 errores, 0 warnings
dotnet test OrderFlow.slnx --verbosity normal  # 0 failing

# 4. Commit (usar el mensaje exacto del fichero docs/Codigo/mX.Y.md)
git add .
git commit -m "m2.2: ..."

git push origin m2.2

# 5. Mergear a main
git checkout main
git merge m2.2 --no-ff -m "merge: m2.2 → main"
git push origin main

# 6. Volver a la rama del módulo para continuar trabajando
git checkout m2.2
```

> **Nunca hacer commit si los tests fallan.**
> **Nunca mergear a main si el build tiene errores o warnings.**

---

## Estructura esperada en `start`

```
orderflow/
├── .editorconfig
├── .gitignore
├── CLAUDE.md
├── OrderFlow.slnx
├── docs/
│   └── Codigo/
│       ├── setup.md          ← este fichero
│       ├── m2.1.md
│       ├── m2.2.md
│       └── ...
└── infrastructure/
    ├── OrderFlow.AppHost/
    │   ├── OrderFlow.AppHost.csproj
    │   ├── Program.cs          ← sin servicios registrados
    │   └── appsettings.json    ← LocalDB + RabbitMQ
    └── OrderFlow.ServiceDefaults/
        ├── OrderFlow.ServiceDefaults.csproj
        └── Extensions.cs
```

`src/`, `shared/` y `tests/` no existen en `start` — se crean módulo a módulo.

---

## Referencia rápida — qué añade cada módulo

| Rama | Añade |
|---|---|
| `start` | `.sln` + `AppHost` + `ServiceDefaults` |
| `m2.1` | `Orders.API` skeleton + Aspire orquestando |
| `m2.2` | Options Pattern + Health Checks + User Secrets |
| `m2.3` | Serilog + OpenTelemetry + CorrelationId |
| `m2.4` | Tests: `LocalDbFixture` + Integration Tests |
| `m3.1` | Clean Architecture + Domain Layer |
| `m3.2` | EF Core + Repository Pattern |
| `m3.3` | `Products.API` con Minimal APIs |
| `m3.4` | CQRS con MediatR |
| `m3.5` | Polly + Resiliencia |
| `m3.6` | Lab: consolidación |
| `m4.1` | HttpClient + Refit |
| `m4.2` | RabbitMQ + `Notifications.API` + `OrderFlow.Contracts` |
| `m4.3` | `Payments.API` + Saga Pattern |
| `m4.4` | YARP Gateway |
| `m5.1` | Identity + JWT |
| `m5.2` | Authorization Policies |
| `m5.3` | Rate Limiting + Seguridad avanzada |
| `m6.1` | Migration Bundles |
| `m6.2` | Outbox Pattern |
| `m6.3` | Read Model + Dapper |
| `m6.4` | Performance + Caching |
| `m7.1` | GitHub Actions CI |
| `m7.2` | Azure App Services |
| `m7.3` | CD + Blue-Green |
| `m7.4` | Application Insights |
