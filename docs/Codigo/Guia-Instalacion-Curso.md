# Guía de Instalación — Microservicios con .NET 10

> Sigue esta guía **en orden**. No saltes pasos. Cada verificación al final de cada sección confirma que el software está correctamente instalado antes de continuar.

---

## Cuándo necesitas qué

| Software | Necesario antes de | Tiempo de instalación |
|---|---|---|
| Visual Studio 2022 | Primera sesión (M2.1) | ~30 min |
| .NET 10 SDK | Primera sesión (M2.1) | Incluido con VS |
| SQL Server LocalDB | Primera sesión (M2.1) | Incluido con VS |
| Git | Primera sesión (M2.1) | ~5 min |
| Erlang/OTP | M4.2 | ~5 min |
| RabbitMQ | M4.2 | ~5 min |
| Azure CLI | M7.2 | ~5 min |
| Cuenta de Azure | M7.2 | ~10 min |

**Coste total de software: 0 €.** Todo es gratuito.

---

## FASE 1 — Antes de la primera sesión

### 1. Visual Studio 2022

**Versión mínima requerida:** 17.12

**Descargar:**
👉 https://visualstudio.microsoft.com/downloads/

Seleccionar **Community** (gratuita).

**Durante la instalación, marcar exactamente estos Workloads:**

| Workload | Dónde está |
|---|---|
| ✅ ASP.NET and web development | Pestaña "Workloads" |
| ✅ Data storage and processing | Pestaña "Workloads" |

**En la pestaña "Individual components", buscar y marcar:**

| Componente | Qué instala |
|---|---|
| ✅ .NET Aspire SDK | Orquestador de servicios |
| ✅ SQL Server Express LocalDB | Base de datos local |

> Si ya tienes Visual Studio 2022 instalado: abre el **Visual Studio Installer** → Modify → selecciona los Workloads y componentes anteriores → Modify.

**Verificación:**

Abre Visual Studio 2022. Si la versión en `Help → About` muestra **17.12** o superior, la instalación es correcta.

---

### 2. .NET 10 SDK

El SDK se instala automáticamente con Visual Studio. Para verificar:

```
dotnet --version
```

**Output esperado:** `10.0.x` (cualquier número de patch)

Si no muestra `10.0.x`, descarga el SDK manualmente:
👉 https://dotnet.microsoft.com/download/dotnet/10.0

Descargar **SDK x64** para Windows.

**Verificación:**

```
dotnet --version
```

Debe mostrar `10.0.x`. Si muestra `9.x` o inferior, instala el SDK manualmente desde el enlace anterior.

---

### 3. SQL Server LocalDB

Se instala automáticamente con el Workload "Data storage and processing" de Visual Studio.

**Verificación:**

```
sqllocaldb info
```

**Output esperado:**

```
MSSQLLocalDB
```

Si el comando no existe o no muestra ninguna instancia:

1. Abre **Visual Studio Installer**
2. Modify → Individual components
3. Marca "SQL Server Express LocalDB"
4. Modify

O descarga por separado:
👉 https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb

**Arrancar la instancia si es necesario:**

```
sqllocaldb start MSSQLLocalDB
sqllocaldb info MSSQLLocalDB
```

El campo `State` debe mostrar `Running`.

**Verificación final:**

```
sqllocaldb info MSSQLLocalDB
```

Buscar la línea `State:` → debe decir `Running`.

---

### 4. Git

**Descargar:**
👉 https://git-scm.com/downloads

Seleccionar **Windows** → descargar el instalador de 64 bits.

Durante la instalación, dejar todas las opciones por defecto. Solo asegurarse de que está marcado:
- ✅ "Git Bash Here" (opcional pero útil)
- ✅ "Use Git from the Windows Command Prompt"

**Verificación:**

```
git --version
```

**Output esperado:** `git version 2.x.x`

**Configuración inicial** (si no la has hecho antes):

```
git config --global user.name "Tu Nombre"
git config --global user.email "tu@email.com"
git config --global core.autocrlf true
```

---

### 5. Cuenta de GitHub

**Crear cuenta (si no tienes):**
👉 https://github.com

La cuenta gratuita es suficiente para todo el curso. GitHub Actions incluye 2.000 minutos/mes gratuitos.

---

### ✅ Verificación completa FASE 1

Ejecuta estos 4 comandos. Todos deben responder sin error:

```
dotnet --version
```
→ Debe mostrar `10.0.x`

```
sqllocaldb info
```
→ Debe mostrar `MSSQLLocalDB`

```
git --version
```
→ Debe mostrar `git version 2.x.x`

```
dotnet new webapi -n TestAPI -o C:\Temp\TestAPI --framework net10.0
```
→ Debe crear el proyecto sin errores

Limpieza:
```
rmdir /s /q C:\Temp\TestAPI
```

Si algún comando falla, resuelve ese punto antes de continuar.

---

## FASE 2 — Antes de M4.2 (RabbitMQ)

> No necesitas esto hasta que lleguemos al módulo M4.2. Puedes instalarlo antes si quieres, pero no es bloqueante hasta ese momento.

### 6. Erlang/OTP

RabbitMQ requiere Erlang como runtime. **Instala Erlang primero.**

**Descargar:**
👉 https://www.erlang.org/downloads

Seleccionar el instalador de Windows (64-bit) — archivo `otp_win64_XX.X.exe`.

Ejecutar el instalador con todas las opciones por defecto (Next → Next → Install → Finish).

**Verificación:**

Abre una ventana nueva de Símbolo del sistema (cmd) o PowerShell:

```
erl -version
```

**Output esperado:** `Erlang/OTP XX [erts-XX.X]...`

Si el comando no se reconoce, cierra y vuelve a abrir el terminal — el PATH necesita actualizarse.

---

### 7. RabbitMQ

**Descargar:**
👉 https://www.rabbitmq.com/docs/install-windows

Seleccionar el instalador de Windows — archivo `rabbitmq-server-X.XX.X.exe`.

Ejecutar el instalador con todas las opciones por defecto.

RabbitMQ se instala como **servicio de Windows** y arranca automáticamente con el sistema.

**Habilitar el panel de gestión web:**

Abre el **RabbitMQ Command Prompt** (busca "RabbitMQ" en el menú Inicio):

```
rabbitmq-plugins enable rabbitmq_management
```

**Output esperado:**

```
Enabling plugins on node rabbit@HOSTNAME:
rabbitmq_management
...
started X plugins.
```

Puede ser necesario reiniciar el servicio:

```
rabbitmqctl stop_app
rabbitmqctl start_app
```

**Verificación de funcionamiento:**

```
rabbitmqctl status
```

Buscar la línea `Status of node rabbit@HOSTNAME` → debe mostrar `running`.

**Verificación del panel web:**

Abre el navegador en: 👉 http://localhost:15672

- Usuario: `guest`
- Contraseña: `guest`

Debe aparecer el dashboard de gestión de RabbitMQ con las pestañas Overview, Connections, Channels, etc.

**Connection string para .NET (para referencia):**

```
amqp://guest:guest@localhost:5672
```

---

### ✅ Verificación completa FASE 2

```
rabbitmqctl status
```
→ Debe mostrar `running`

Navegar a http://localhost:15672 con `guest` / `guest`
→ Debe mostrar el dashboard

---

## FASE 3 — Antes de M7.2 (Azure)

> No necesitas esto hasta el módulo M7.2.

### 8. Cuenta de Azure

**Opciones:**

| Opción | Crédito | Requisito |
|---|---|---|
| Azure Free Trial | 200 $ (30 días) | Tarjeta de crédito (no se cobra) |
| Azure for Students | 100 $ sin tarjeta | Email educativo |
| Pay-as-you-go | Sin crédito gratuito | Solo pagas lo que usas |

Registrarse en: 👉 https://azure.microsoft.com/free/

> **Coste estimado del curso:** Si creas los recursos de Azure solo para las sesiones del Módulo 7 y los eliminas al terminar, el coste es inferior a 20 €.

---

### 9. Azure CLI

**Descargar:**
👉 https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows

Descargar el instalador MSI (64-bit). Ejecutar con opciones por defecto.

**Verificación:**

```
az --version
```

**Output esperado:** Muestra `azure-cli X.XX.X` en la primera línea.

**Login:**

```
az login
```

Abre el navegador automáticamente. Inicia sesión con tu cuenta de Azure.

---

### ✅ Verificación completa FASE 3

```
az --version
```
→ Debe mostrar `azure-cli X.XX.X`

```
az account show
```
→ Debe mostrar tu suscripción activa de Azure

---

## Resumen de verificación global

Antes de la primera sesión, ejecuta este bloque completo. Todo debe responder correctamente:

```
echo === .NET SDK ===
dotnet --version

echo === SQL Server LocalDB ===
sqllocaldb info

echo === Git ===
git --version
```

Antes de M4.2:

```
echo === RabbitMQ ===
rabbitmqctl status
```

Antes de M7.2:

```
echo === Azure CLI ===
az --version
```

---

## Software que NO hay que instalar

| Software | Por qué NO |
|---|---|
| Docker Desktop | No se usa en el curso. Aspire orquesta los servicios .NET directamente. RabbitMQ y SQL Server se instalan de forma nativa. |
| Kubernetes (minikube, kind...) | Solo se explica como concepto teórico en M7.1. No hay laboratorio práctico. |
| Terraform / Pulumi | Usamos Azure CLI y el portal de Azure. |
| Grafana / Prometheus / Loki | Usamos Azure Monitor + Application Insights en producción. |
| MongoDB / PostgreSQL / Redis | El curso usa SQL Server exclusivamente. |
| Node.js / npm | No hay frontend JavaScript en el curso. |
| Postman | Los ficheros `.http` integrados en Visual Studio 2022 son suficientes. Postman es opcional. |

---

## Problemas frecuentes

**`sqllocaldb info` no devuelve nada o da error:**
→ Abre Visual Studio Installer → Modify → Individual components → marca "SQL Server Express LocalDB" → Modify.

**`erl -version` no se reconoce después de instalar Erlang:**
→ Cierra todos los terminales y ábrelos de nuevo. Si persiste, añade manualmente `C:\Program Files\Erlang OTP\bin` al PATH del sistema.

**RabbitMQ no arranca (servicio detenido):**
→ Abre Servicios de Windows (services.msc) → busca "RabbitMQ" → Start. Si da error, verifica que Erlang está instalado correctamente con `erl -version`.

**`rabbitmq-plugins enable rabbitmq_management` da error de acceso:**
→ Abre el RabbitMQ Command Prompt como **Administrador** (click derecho → Ejecutar como administrador).

**`dotnet --version` muestra 9.x en vez de 10.x:**
→ Descarga el SDK de .NET 10 manualmente desde https://dotnet.microsoft.com/download/dotnet/10.0 y reinstala.

**El panel web de RabbitMQ (localhost:15672) no carga:**
→ Verifica que el servicio está corriendo con `rabbitmqctl status`. Si está corriendo pero no carga, espera 30 segundos después de habilitar el plugin y recarga la página.
