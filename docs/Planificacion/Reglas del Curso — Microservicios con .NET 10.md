# Reglas del Curso — Microservicios con .NET 10

Documento de referencia para mantener coherencia en todas las presentaciones y ficheros del curso. Última actualización: Abril 2026

---

## 🏗️ Arquitectura y código

|Regla|✅ Correcto|❌ Prohibido|
|---|---|---|
|Arquitectura|**Clean Architecture** (Domain, Application, Infrastructure, API)|~~Hexagonal~~ / ~~Ports & Adapters~~|
|Orquestador local|**.NET Aspire** con `AddConnectionString()`|~~Docker Compose~~|
|BD en desarrollo|**SQL Server LocalDB**|~~Docker~~ / ~~contenedores~~|
|BD en tests|**LocalDbFixture** con LocalDB|~~TestContainers~~|
|Broker mensajería local|**RabbitMQ nativo Windows** (`rabbitmqctl status`)|~~RabbitMQ en Docker~~|
|Identity Provider|**Keycloak nativo** (`bin\kc.bat start-dev --http-port=8180`)|~~Keycloak en Docker~~|
|Despliegue|**Azure App Services**|~~Kubernetes~~ / ~~contenedores~~|
|Aspire recursos|`builder.AddConnectionString(...)`|~~`AddRabbitMQ()`~~ / ~~`AddSqlServer()`~~ con Docker|

---

## 🧪 Testing y CI/CD

|Regla|✅ Correcto|❌ Prohibido|
|---|---|---|
|BD en tests integración|LocalDB (`Server=(localdb)\\MSSQLLocalDB`)|~~TestContainers~~|
|Pipeline CI runner|`windows-latest` (LocalDB disponible)|~~`ubuntu-latest`~~ para tests con BD|
|macOS/Linux alternativa BD|Azure SQL Database o SQL Server Express|~~SQL Server en Docker~~|

---

## 🗂️ Documentación y plataforma

|Regla|Detalle|
|---|---|
|Plataforma|**Obsidian** — se descargan los `.md` y se copian al vault|
|Notion|❌ No se usa — no subir nada|
|Cada `##`|Una diapositiva en Gamma|
|Mínimo por presentación|**55 slides**|
|Idioma|Español de España (vosotros, deploys, implementar…)|
|Verificación Docker|Scan automático antes de copiar cada fichero generado|

---

## 🏪 Proyecto OrderFlow / TechShop

|Elemento|Detalle|
|---|---|
|Empresa ficticia|**TechShop**|
|Proyecto|**OrderFlow**|
|Servicios|Orders.API (Controllers + CQRS) · Products.API (Minimal APIs) · Payments.API · Notifications.API · Gateway.API (YARP)|
|Bases de datos|OrdersDb · ProductsDb · PaymentsDb · NotificationsDb — todas SQL Server LocalDB en desarrollo|
|Mensajería local|RabbitMQ nativo Windows → Azure Service Bus en producción (1 línea de cambio en MassTransit)|
|Identity local|Keycloak nativo en :8180 → Azure AD en producción|
|Arquitectura código|**Clean Architecture** en todos los servicios|
|Stack tecnológico|.NET 10 LTS · C# 14 · ASP.NET Core 10 · EF Core 10 · MassTransit · Polly v8 · Serilog · OpenTelemetry|

---

## 📋 Estructura de carpetas (Clean Architecture)

```
NombreServicio.API/
├── Domain/                  ← Entidades, Value Objects, Aggregates, Domain Events
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/
│   ├── Exceptions/
│   └── Interfaces/          ← IRepository (definido aquí, implementado en Infrastructure)
├── Application/             ← Use Cases: Commands, Queries, Handlers, Behaviors
│   ├── Commands/
│   ├── Queries/
│   ├── Behaviors/           ← Logging, Validation, Transaction
│   ├── DTOs/
│   └── Exceptions/
├── Infrastructure/          ← Implementaciones externas: BD, HTTP, Mensajería
│   ├── Persistence/         ← DbContext, Repositories, Migrations
│   └── Http/                ← Typed Clients (ProductsClient, etc.)
└── API/                     ← Controllers, Middleware, Mappings
    ├── Controllers/
    ├── DTOs/
    ├── Mappings/
    └── Middleware/
```

**La Dependency Rule (Uncle Bob):** el código solo apunta hacia adentro.

- Domain no conoce ni Application, ni Infrastructure, ni API
- Application conoce Domain pero no Infrastructure ni API
- Infrastructure conoce Domain y Application
- API conoce todo (es el punto de composición)

---

## ⚙️ Comandos de verificación rápida

```bash
# Verificar que no hay referencias a Docker problemáticas
grep -ri "testcontainer\|dockerfile" src/ | grep -iv "sin docker\|no docker\|descartado"

# Verificar que no hay referencias a Hexagonal
grep -ri "hexagonal\|ports.*adapters\|puerto.*adaptador" src/

# Verificar que LocalDB está en marcha
sqllocaldb info MSSQLLocalDB

# Verificar RabbitMQ nativo
rabbitmqctl status

# Verificar Keycloak nativo
curl http://localhost:8180/realms/techshop/.well-known/openid-configuration
```

---

## 🚫 Correcciones históricas aplicadas

Errores detectados y corregidos durante la generación del curso:

|Fichero(s)|Error original|Corrección|
|---|---|---|
|M2.4, M2.3|`TestContainers` / `MsSqlContainer` en tests|`LocalDbFixture` con SQL Server LocalDB|
|M3.1, M2.1, M2.4, M3.5, M6.1|`arquitectura hexagonal`|`Clean Architecture`|
|Gamma-Configuracion|`TestContainers` en M2.4, `hexagonal` en M3.1|Corregido|
|ROADMAP|Sección entera de hexagonal + TestContainers|Corregido a Clean Architecture + LocalDB|
|Requisitos-Software|`SQL Server en Docker` para macOS|Azure SQL Database o SQL Server Express|
|M3.6 pipeline CI|`ubuntu-latest` con comentario TestContainers|`windows-latest` con LocalDB|

---

## 📅 Estado de generación de presentaciones

|Módulo|Presentaciones|Estado|
|---|---|---|
|M1 — Introducción|M1.1 · M1.2 · M1.3|✅ Completo|
|M2 — Fundamentos .NET 10|M2.1 · M2.2 · M2.3 · M2.4|✅ Completo|
|M3 — Desarrollo Microservicios|M3.1 · M3.2 · M3.3 · M3.4 · M3.5 · M3.6|✅ Completo|
|M4 — Comunicación|M4.1 · M4.2 · M4.3 · M4.4|✅ Completo|
|M5 — Seguridad|M5.1 · M5.2 · M5.3|✅ Completo|
|M6 — Gestión de Datos|M6.1 · M6.2 · M6.3 · M6.4|🔄 En curso (M6.1 y M6.2 listos)|
|M7 — Despliegue|M7.1 · M7.2 · M7.3 · M7.4|⏳ Pendiente|