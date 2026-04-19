# Requisitos de Software — Microservicios con .NET 10

## Curso de 25 horas · Formación técnica

---

## Resumen rápido

|Software|Versión mínima|¿Obligatorio?|Coste|¿Cuándo se usa?|
|---|---|---|---|---|
|Visual Studio 2022|17.12+|✅ Sí|Gratuito (Community)|Todo el curso|
|.NET 10 SDK|10.0|✅ Sí|Gratuito (incluido en VS)|Todo el curso|
|SQL Server LocalDB|2022|✅ Sí|Gratuito (incluido en VS)|Desde M2.1|
|Erlang/OTP|26.x|✅ Sí|Gratuito|Necesario para RabbitMQ|
|RabbitMQ|3.13+|✅ Sí|Gratuito|Desde M4.2|
|Git|2.x|✅ Sí|Gratuito|Todo el curso|
|Cuenta de GitHub|—|✅ Sí|Gratuito|Repo del proyecto + CI/CD (M7.3)|
|Cuenta de Azure|—|⚠️ Desde M7.2|Ya disponible|Módulo 7 (despliegue)|
|Azure Data Studio|Última|📋 Recomendado|Gratuito|Consultar SQL Server|
|Postman o Bruno|Última|📋 Opcional|Gratuito|Probar APIs|
|Azure CLI|Última|⚠️ Desde M7.2|Gratuito|Gestión de Azure|

**Coste total de herramientas (sin Azure): 0€.** Todo es gratuito.

---

## 1. Visual Studio 2022 (obligatorio)

**Versión:** 17.12 o superior (necesaria para .NET 10 y Aspire)

**Edición:** Community (gratuita), Professional o Enterprise

**Descargar:** https://visualstudio.microsoft.com/downloads/

**Workloads a instalar durante el setup:**

|Workload|¿Por qué?|
|---|---|
|ASP.NET and web development|APIs, Minimal APIs, Controllers, Swagger|
|.NET Aspire (componente individual)|Orquestación de servicios en desarrollo|
|Data storage and processing|SQL Server LocalDB, herramientas de datos|

**Componentes individuales recomendados** (pestaña "Individual components"):

- .NET 10 SDK
- SQL Server LocalDB
- SQL Server Express LocalDB (runtime)
- .NET Aspire SDK

**Nota:** Si el alumno ya tiene Visual Studio 2022 instalado, puede añadir los workloads desde el Visual Studio Installer sin reinstalar.

**Alternativa:** Visual Studio Code + C# Dev Kit es viable pero tiene menos soporte integrado para Aspire y debugging multi-servicio. Para este curso, Visual Studio 2022 es la opción más productiva.

---

## 2. .NET 10 SDK (obligatorio)

**Versión:** 10.0 (LTS — soporte hasta noviembre 2028)

**Descargar:** https://dotnet.microsoft.com/download/dotnet/10.0

**Verificar instalación:**

```
dotnet --version
```

Debe mostrar `10.0.x`

**Nota:** Si se instala Visual Studio 2022 v17.12+ con el workload de ASP.NET, el SDK de .NET 10 se incluye automáticamente. No es necesario instalarlo por separado.

---

## 3. SQL Server LocalDB (obligatorio)

**Qué es:** Una versión ligera de SQL Server diseñada para desarrollo. Se ejecuta bajo demanda (no como servicio permanente), consume pocos recursos y no requiere configuración.

**Versión:** SQL Server 2022 LocalDB

**Instalación:** Se instala automáticamente con Visual Studio 2022 si se selecciona el workload "Data storage and processing". También se puede instalar por separado.

**Descargar por separado (si no viene con VS):** https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb

**Verificar instalación:**

```
sqllocaldb info
```

Debe mostrar al menos una instancia (normalmente `MSSQLLocalDB`).

**Connection string típica:**

```
Server=(localdb)\MSSQLLocalDB;Database=OrdersDb;Trusted_Connection=true;
```

**Nota:** LocalDB no requiere usuario/contraseña — usa autenticación de Windows integrada. Ideal para desarrollo, cero configuración.

---

## 4. Erlang/OTP + RabbitMQ (obligatorio desde M4.2)

**Qué es:** RabbitMQ es el message broker que usamos para comunicación asíncrona entre microservicios. Necesita Erlang como runtime (igual que .NET necesita el .NET runtime).

**Coste:** Gratuito. Ambos son open source.

### Paso 1: Instalar Erlang/OTP

**Descargar:** https://www.erlang.org/downloads

Descargar el instalador de Windows (64-bit). Ejecutar con opciones por defecto. La instalación es tipo "Next, Next, Finish".

**Verificar:**

```
erl -version
```

### Paso 2: Instalar RabbitMQ

**Descargar:** https://www.rabbitmq.com/docs/install-windows

Descargar el instalador de Windows. Ejecutar con opciones por defecto. RabbitMQ se instala como servicio de Windows — arranca automáticamente.

**Habilitar el panel de gestión web (recomendado):**

```
rabbitmq-plugins enable rabbitmq_management
```

Después acceder a http://localhost:15672 con usuario `guest` / contraseña `guest`.

**Verificar que RabbitMQ está corriendo:**

```
rabbitmqctl status
```

**Connection string para .NET:**

```
amqp://guest:guest@localhost:5672
```

### Configurar .NET Aspire para usar RabbitMQ local

Aspire puede conectar a un RabbitMQ ya instalado en vez de levantar un contenedor. En el proyecto AppHost:

```csharp
// En vez de: var rabbitmq = builder.AddRabbitMQ("messaging");
// Usar conexión al RabbitMQ local:
var rabbitmq = builder.AddConnectionString("messaging");
```

Con la connection string en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "messaging": "amqp://guest:guest@localhost:5672"
  }
}
```

**Nota:** No hace falta instalar RabbitMQ hasta el Módulo 4 (sesión M4.2). Los Módulos 1, 2 y 3 solo usan SQL Server LocalDB.

---

## 5. Git (obligatorio)

**Versión:** 2.x o superior

**Descargar:** https://git-scm.com/downloads

**Verificar instalación:**

```
git --version
```

**Configuración inicial:**

```
git config --global user.name "Tu Nombre"
git config --global user.email "tu@email.com"
```

**Nota:** Visual Studio 2022 incluye una instalación de Git integrada, pero es recomendable tener Git instalado a nivel de sistema para usar la línea de comandos.

---

## 6. Cuenta de GitHub (obligatorio)

**Para qué:** Repositorio del proyecto del curso y CI/CD con GitHub Actions (Módulo 7).

**Crear cuenta:** https://github.com (gratuita)

**Lo que necesitamos:**

- Un repositorio (público o privado) para el proyecto OrderFlow
- GitHub Actions (incluido gratis con 2.000 minutos/mes en cuentas gratuitas)
- En M7.3, configuraremos el pipeline de CI/CD contra este repositorio

---

## 7. Cuenta de Azure (obligatorio desde el Módulo 7)

**Para qué:** Desplegar los microservicios en Azure App Services, Azure SQL Database y Azure Service Bus.

**Opciones:**

|Opción|Coste|Nota|
|---|---|---|
|Azure Free Trial|200$ crédito por 30 días|Ideal si es la primera vez|
|Azure for Students|100$ crédito sin tarjeta|Requiere email educativo|
|Pay-as-you-go|Pago por uso|Para el curso, el coste estimado es <20€ si se eliminan los recursos al terminar|
|Visual Studio Subscription|50-150$/mes en créditos|Si el alumno tiene suscripción VS Professional/Enterprise|

**Crear cuenta:** https://azure.microsoft.com/free/

**Servicios de Azure que usaremos:**

|Servicio|Tier del curso|Coste estimado|
|---|---|---|
|App Service (×4 servicios + gateway)|B1 (Basic)|~5 × 13€/mes|
|Azure SQL Database|Basic (5 DTU) × 4|~4 × 5€/mes|
|Azure Service Bus|Basic|~0.05€/mes|
|Application Insights|Free tier (5 GB/mes)|0€|
|Azure Key Vault|Standard|~0.03€/mes|

**Coste total estimado:** ~85€/mes si se deja corriendo. Si se crean los recursos para la práctica y se eliminan al terminar, el coste puede ser <20€.

**Recomendación:** Crear los recursos de Azure solo cuando lleguemos al Módulo 7. No es necesario antes.

---

## 8. Azure Data Studio (recomendado)

**Qué es:** Editor de consultas SQL gratuito de Microsoft. Más ligero que SQL Server Management Studio (SSMS), multiplataforma.

**Para qué:** Ejecutar consultas contra LocalDB y Azure SQL Database, inspeccionar tablas, verificar migrations.

**Descargar:** https://learn.microsoft.com/en-us/azure-data-studio/download

**Alternativa:** SQL Server Management Studio (SSMS) — más completo pero solo Windows y más pesado. Descargar: https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms

**Otra alternativa:** La extensión SQL Server de Visual Studio 2022 (Server Explorer) permite consultar bases de datos sin salir del IDE.

---

## 9. Postman o Bruno (recomendado)

**Para qué:** Probar APIs manualmente, enviar peticiones HTTP con headers, body, autenticación.

**Opciones:**

|Herramienta|Coste|Nota|
|---|---|---|
|Postman|Gratuito (con cuenta)|El más popular. Descarga: https://www.postman.com/downloads/|
|Bruno|Gratuito y open source|Sin cuenta, offline. Descarga: https://www.usebruno.com/|
|REST Client (extensión VS Code)|Gratuito|Ficheros .http directamente en el editor|
|.http files en Visual Studio 2022|Incluido|Endpoints Explorer + ficheros .http integrados|

**Recomendación:** Los ficheros `.http` de Visual Studio 2022 son suficientes para el curso y no requieren instalar nada adicional. Postman o Bruno son opcionales para quien prefiera una herramienta dedicada.

---

## 10. Azure CLI (obligatorio desde el Módulo 7)

**Para qué:** Gestionar recursos de Azure desde la línea de comandos. Crear App Services, configurar Azure SQL, desplegar.

**Descargar:** https://learn.microsoft.com/en-us/cli/azure/install-azure-cli

**Verificar instalación:**

```
az --version
```

**Login:**

```
az login
```

**Nota:** También se puede usar el portal de Azure (interfaz web) para crear recursos, pero Azure CLI es más rápido y reproducible. En el Módulo 7 usaremos ambos.

---

## Requisitos de hardware del equipo

|Componente|Mínimo|Recomendado|
|---|---|---|
|RAM|8 GB|16 GB|
|Disco libre|15 GB|30 GB|
|CPU|4 cores|8 cores|
|SO|Windows 10 64-bit (v2004+)|Windows 11|
|Pantalla|1920×1080|2 monitores o ultrawide|

**Nota sobre RAM:** Visual Studio (~2 GB) + Aspire con 4 servicios .NET (~1.5 GB) + SQL Server LocalDB (~500 MB) + RabbitMQ (~200 MB) consumen unos 4-5 GB. Con 8 GB de RAM funciona cómodamente. Sin Docker, el consumo de recursos es mucho menor.

**Nota sobre SO:** macOS y Linux son viables con Visual Studio Code + C# Dev Kit, pero el curso está diseñado para Visual Studio 2022 en Windows. Los alumnos con macOS/Linux pueden seguir el curso pero tendrán que adaptar algunos pasos (LocalDB no existe en macOS — alternativa: Azure SQL Database (recomendado) o SQL Server Express (gratuito)).

---

## Alternativas para situaciones especiales

**Si no se puede instalar RabbitMQ (políticas de empresa, permisos):**

- Opción A: Usar Azure Service Bus desde el primer día (requiere cuenta Azure, coste mínimo ~0.05€/mes)
- Opción B: Simular la mensajería con MediatR en memoria (solo para desarrollo local, no recomendado)
- MassTransit abstrae el broker — el código del alumno no cambia

**Si no se puede usar LocalDB:**

- Opción A: SQL Server Express (gratuito): https://www.microsoft.com/en-us/sql-server/sql-server-downloads
- Opción B: Azure SQL Database desde el primer día (requiere cuenta Azure)

**Si el alumno usa macOS o Linux:**

- Visual Studio Code + C# Dev Kit + .NET CLI
- SQL Server: Azure SQL Database (recomendado) o SQL Server Express (gratuito): https://www.microsoft.com/en-us/sql-server/sql-server-downloads
- RabbitMQ: se instala nativamente en macOS (`brew install rabbitmq`) y Linux (`apt install rabbitmq-server`)

---

## Checklist de verificación previa al curso

El alumno debe poder ejecutar estos comandos sin errores antes de la primera sesión:

```
✅ dotnet --version                    → Debe mostrar 10.0.x
✅ git --version                       → Debe mostrar 2.x
✅ sqllocaldb info                     → Debe mostrar al menos una instancia
✅ az --version                        → Debe mostrar azure-cli 2.x (solo para M7+)
```

**Verificación de RabbitMQ (solo necesario antes del Módulo 4):**

```
✅ rabbitmqctl status                  → Debe mostrar que está corriendo
✅ http://localhost:15672               → Panel de gestión (guest/guest)
```

**Test de Visual Studio:**

1. Abrir Visual Studio 2022
2. Crear nuevo proyecto → "ASP.NET Core Web API"
3. Seleccionar .NET 10
4. Ejecutar (F5) → Debe abrir Swagger UI en el navegador

Si algún paso falla, resolver ANTES de la formación. Los primeros 30 minutos de un curso perdidos en instalaciones frustran a todo el mundo.

---

## Software que NO necesitamos instalar

Para evitar confusiones, esto es lo que NO hace falta:

|Software|¿Por qué no?|
|---|---|
|Docker Desktop|Aspire orquesta los proyectos .NET directamente; RabbitMQ y SQL Server se instalan nativamente|
|Kubernetes (minikube, kind, etc.)|Solo lo vemos como concepto teórico|
|Helm|Solo concepto teórico|
|Terraform / Pulumi|Usamos Azure CLI y portal|
|Grafana / Prometheus / Loki / Tempo|Usamos Azure Monitor + Application Insights|
|MongoDB|Usamos SQL Server exclusivamente|
|PostgreSQL|Usamos SQL Server exclusivamente|
|Redis|Usamos SQL Server + IMemoryCache|
|Consul / Eureka|Aspire gestiona service discovery en local|
|Node.js / npm|No usamos frontend JavaScript|
|Visual Studio Code|Opcional — el curso usa Visual Studio 2022|