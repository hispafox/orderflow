# Setup local · OrderFlow

Guía para dejar un equipo Windows listo para ejecutar la solución completa (backend Aspire + SPA demo).

> Esta guía **no** usa Docker. Todos los servicios de infraestructura se instalan nativamente en Windows.

---

## 1. Software obligatorio

| Componente | Versión mínima | Verificación |
|---|---|---|
| Windows | 10 / 11 | `winver` |
| .NET SDK | 10.0 preview | `dotnet --list-sdks` |
| SQL Server LocalDB | 15+ (con VS 2022) | `sqllocaldb info MSSQLLocalDB` |
| RabbitMQ Server | 3.12+ | servicio "RabbitMQ" en `services.msc` |
| Erlang/OTP | 26+ (dep. de RabbitMQ) | se instala con RabbitMQ |
| Node.js | 20 LTS+ | `node --version` |
| Git | 2.40+ | `git --version` |

### 1.1 .NET SDK

Descarga desde <https://dotnet.microsoft.com/download/dotnet/10.0>. Tras instalar:

```powershell
dotnet --version           # debe devolver 10.0.x-preview
dotnet workload install aspire
```

El SDK ya incluye el cert de desarrollo HTTPS. Si nunca lo has confiado:

```powershell
dotnet dev-certs https --trust
```

### 1.2 SQL Server LocalDB

La forma fácil: instalar **Visual Studio 2022** con el workload *"ASP.NET and web development"* (marca la opción *SQL Server LocalDB*).

Alternativa independiente: **SQL Server Express** desde <https://www.microsoft.com/sql-server/sql-server-downloads> (elige "Express", el instalador incluye LocalDB).

Verificación:
```powershell
sqllocaldb info MSSQLLocalDB
# si la instancia no existe:
sqllocaldb create "MSSQLLocalDB"
sqllocaldb start  "MSSQLLocalDB"
```

Connection string por defecto: `Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=true;TrustServerCertificate=true`.

### 1.3 RabbitMQ (nativo, sin Docker)

**Opción A — Chocolatey (recomendada):**

Si no tienes Chocolatey instalado, primero instálalo (una sola vez):

1. Abre **PowerShell como administrador** (click derecho en el icono → *Ejecutar como administrador*).
2. Ejecuta:
   ```powershell
   Set-ExecutionPolicy Bypass -Scope Process -Force
   [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
   iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
   ```
3. Cierra y vuelve a abrir PowerShell (como admin) para refrescar el `PATH`.
4. Verifica: `choco --version` → debe devolver una versión.

Fuente oficial: <https://chocolatey.org/install>.

> **Si `Set-ExecutionPolicy` falla o da una advertencia**, suele ser por una Group Policy o por un mensaje de confirmación:
>
> - *"Set-ExecutionPolicy : Windows PowerShell updated your execution policy successfully, but the setting is overridden by a policy…"* — tu empresa ha fijado la política por GPO. El comando anterior usa `-Scope Process`, que solo afecta a la sesión actual y no requiere cambiar la política del sistema, así que **puedes ignorar el mensaje y continuar**.
> - Diagnóstico: `Get-ExecutionPolicy -List` muestra la política efectiva por scope. Si `MachinePolicy` o `UserPolicy` están en `AllSigned` o `Restricted` no se pueden sobrescribir, pero `Process` sí.
> - Si te pide confirmación (Y/N), responde `Y` o añade `-Force` al comando.
> - Como último recurso, descarga el instalador y ejecútalo manualmente:
>   ```powershell
>   Invoke-WebRequest -Uri https://community.chocolatey.org/install.ps1 -OutFile $env:TEMP\choco-install.ps1
>   powershell -ExecutionPolicy Bypass -File $env:TEMP\choco-install.ps1
>   ```

Con Chocolatey listo, instala Erlang y RabbitMQ (PowerShell **admin**):
```powershell
choco install erlang -y
choco install rabbitmq -y
```

**Opción B — Instaladores MSI:**
1. Erlang/OTP: <https://www.erlang.org/downloads> (descarga el MSI de 64-bit).
2. RabbitMQ Server: <https://www.rabbitmq.com/install-windows.html> (MSI oficial).
3. Al terminar, abre *PowerShell como admin*:
   ```powershell
   cd "C:\Program Files\RabbitMQ Server\rabbitmq_server-<version>\sbin"
   .\rabbitmq-plugins.bat enable rabbitmq_management
   net start RabbitMQ
   ```

Verificación:
- El servicio `RabbitMQ` está en estado *Running* (`services.msc`).
- Puerto AMQP `5672` responde.
- Consola de management: <http://localhost:15672> (usuario/contraseña por defecto: `guest` / `guest`).

Connection string usada por el proyecto: `amqp://guest:guest@localhost:5672`.

> Si **no puedes/quieres** instalar RabbitMQ, existe un flag `InMemory` por servicio.
> Ver detalles en [messaging-transport-switch.md](messaging-transport-switch.md).
>
> **Limitación importante:** en modo InMemory **no funcionan los Sagas cross-service**
> (los eventos no cruzan procesos). La demo end-to-end necesita RabbitMQ.
> InMemory solo sirve para desarrollar un servicio aislado.

### 1.4 Node.js

Descarga Node.js 20 LTS desde <https://nodejs.org>. Verifica:
```powershell
node --version    # v20.x
npm --version     # 10.x
```

### 1.5 Git

<https://git-scm.com/download/win>. En la instalación, marca "Git from the command line and also from 3rd-party software".

---

## 2. Clonar y restaurar

```powershell
git clone https://github.com/hispafox/orderflow.git
cd orderflow

dotnet restore OrderFlow.slnx
dotnet build   OrderFlow.slnx   # debe compilar con 0 errores
```

> **Importante sobre MassTransit:** los `.csproj` fijan `MassTransit 8.5.2`.
> No actualices a 9.x — requiere licencia comercial (`MT_LICENSE`) y el
> AppHost fallará al arrancar con `MassTransit.ConfigurationException: License must be specified`.

### 2.1 User Secrets (para valores sensibles)

Ninguna clave es obligatoria en dev, pero si alguna vez necesitas sobreescribir una connection string:
```powershell
cd src\Orders.API
dotnet user-secrets set "ConnectionStrings:sqlserver" "Server=(localdb)\MSSQLLocalDB;Database=OrderFlowOrders;Trusted_Connection=true;TrustServerCertificate=true"
```

### 2.2 Dependencias del frontend

```powershell
cd web
npm install
```

---

## 3. Arranque

### Opción A — Aspire (recomendada)

Dos terminales:

```powershell
# Terminal 1 — backend completo
dotnet run --project infrastructure\OrderFlow.AppHost --launch-profile https

# Terminal 2 — SPA demo
cd web
npm run dev
```

- Dashboard de Aspire: <https://localhost:17149> (sin login si tienes `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`)
- SPA: <http://localhost:5173>

### Opción B — DevLauncher

Si usas la herramienta DevLauncher, el perfil está en `.devlauncher/profiles/orderflow.json`. Lanza el perfil "orderflow" y arranca los 2 procesos juntos.

---

## 4. Verificación

1. Dashboard de Aspire: todas las filas en estado **Running** (5 servicios + RabbitMQ).
2. `https://localhost:7153/scalar/v1` → Scalar de Orders.API.
3. `https://localhost:7200/scalar/v1` → Scalar de Products.API.
4. `http://localhost:5173` → SPA demo, página Home con KPIs no nulos.

---

## 5. Problemas comunes

| Síntoma | Causa | Solución |
|---|---|---|
| `License must be specified` al arrancar un servicio | MassTransit 9 colado | Fijar todos los `.csproj` a `8.5.2` y `dotnet restore` |
| Dashboard pide token | Variable anónima no activa | Añadir `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` al `launchSettings.json` del AppHost |
| La SPA se queda en "Consultando servicios…" | Proxy de Vite no alcanza al backend | Verificar que Orders.API escucha en 7153 y Products.API en 7200 (pineo de puertos en `AppHost/Program.cs`) |
| Servicio falla con `RabbitMQ connection refused` | RabbitMQ parado | `net start RabbitMQ` o usar transport InMemory |
| `Cannot open database "OrderFlowOrders"` | LocalDB no arrancado | `sqllocaldb start MSSQLLocalDB` |
| Puerto 7153 / 7200 ocupado | Otra instancia corriendo | Mata el proceso: `netstat -ano \| findstr 7153` + `taskkill /PID <pid> /F` |
| `dev-certs` inválido en el navegador | Dev cert no confiado | `dotnet dev-certs https --trust` |

---

## 6. Reglas del proyecto que afectan al setup

- **No usar Docker** — LocalDB y RabbitMQ se instalan nativamente.
- **No usar `builder.AddSqlServer()` ni `builder.AddRabbitMQ()`** en el AppHost — siempre `AddConnectionString()`.
- **No actualizar MassTransit a 9.x** — pinneado a 8.5.2 por el tema de licencia.
- El resto de reglas están en [CLAUDE.md](../CLAUDE.md) sección "LO QUE NUNCA DEBES HACER".
