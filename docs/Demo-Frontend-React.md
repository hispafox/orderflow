# Demo Frontend · React + Vite + TypeScript

> **Módulo extra — NO pertenece a la numeración del curso.** Se mantiene en la rama `frontend` y se fusiona a `main` bajo demanda.

## Objetivo

Proporcionar una SPA ligera para **visualizar en vivo** cómo cooperan los microservicios de OrderFlow cuando se crea y procesa un pedido. Permite demostrar en clase o en una reunión:

- El catálogo que expone `Products.API`.
- El ciclo completo de un pedido gestionado por `Orders.API`.
- La **orquestación del Saga** leyendo el endpoint `GET /api/orders/{id}/saga-state` con polling.

La demo es puramente *read+mutate* sobre los endpoints ya existentes. **No requiere cambios en el backend.**

## Ubicación

- Código fuente: [web/](../web/)
- Rama: `frontend` (derivada de `main`)

## Stack

| Tecnología | Versión | Para qué |
|---|---|---|
| Vite | 5.x | Build tool + dev server con HMR |
| React | 18.x | UI |
| TypeScript | 5.x | Tipado estricto |
| react-router-dom | 6.x | 5 rutas cliente |
| @tanstack/react-query | 5.x | Fetch + caché + polling + invalidación |
| Tailwind CSS | 3.x | Estilos utilitarios |

Se eligió **TanStack Query** porque el polling del Saga y la invalidación tras `confirm`/`cancel` salen del tirón sin boilerplate.

## Arquitectura

### Comunicación con el backend

- **No pasa por el Gateway YARP.** Llama directo a `Orders.API` y `Products.API`.
- **Proxy de Vite** (`web/vite.config.ts`) con `secure: false` para el dev cert de .NET, siguiendo la regla del CLAUDE.md global:

```ts
server: {
  proxy: {
    '/api/products': { target: 'https://localhost:7200', changeOrigin: true, secure: false },
    '/api/orders':   { target: 'https://localhost:7153', changeOrigin: true, secure: false },
  }
}
```

### Autenticación

Ninguna. `OrdersController` está marcado `[AllowAnonymous]` y cuando no hay claim usa un `DemoUserId`. Se deja preparado para añadir JWT cuando se integre con M5.x.

### Tipos TypeScript

`web/src/api/types.ts` replica uno a uno los records del backend: `OrderDto`, `OrderSummaryDto`, `OrderLineDto`, `OrderAddressDto`, `PagedResult<T>`, `ProductResponse`, `ProductSummaryResponse`, `CreateOrderRequest`, `CancelOrderRequest`, `SagaState`.

## Rutas de la SPA

| Ruta | Componente | Endpoints |
|---|---|---|
| `/` | `HomePage` | `GET /api/products?pageSize=1`, `GET /api/orders?pageSize=1` (KPIs) |
| `/products` | `ProductsPage` | `GET /api/products` paginado |
| `/orders` | `OrdersPage` | `GET /api/orders?status=&page=` |
| `/orders/new` | `CreateOrderPage` | `GET /api/products`, `POST /api/orders` |
| `/orders/:id` | `OrderDetailPage` | `GET /api/orders/:id`, `GET /api/orders/:id/saga-state`, `POST /api/orders/:id/confirm`, `POST /api/orders/:id/cancel` |

## Visualización del Saga

El componente `SagaTimeline` representa una timeline vertical con 4 pasos:

1. **Pedido creado** — la orden ya está en `Orders.API`.
2. **Reserva de stock** — `Products.API` retiene unidades (`AwaitingStock`).
3. **Procesamiento de pago** — `Payments.API` valida el cobro (`AwaitingPayment`).
4. **Pedido confirmado** — evento `OrderConfirmed` publicado (`Confirmed`).

El `useQuery` asociado usa `refetchInterval: 2_000` hasta que el estado entra en un valor terminal (`Confirmed`, `Cancelled`, `Failed`). Una pequeña ruedecita junto al título indica cuándo está refetcheando.

## Cómo arrancar la demo

```
Terminal 1:  dotnet run --project infrastructure/OrderFlow.AppHost
Terminal 2:  cd web && npm install   (solo primera vez)
             npm run dev
```

Abre `http://localhost:5173`.

### Requisitos del AppHost para que la demo funcione

Dos ajustes en el AppHost son necesarios para que la SPA pueda consumir los servicios a través del proxy de Vite:

1. **Puertos HTTPS fijos** para Orders.API (7153) y Products.API (7200).
   Aspire, por defecto, ignora `applicationUrl` de cada child project y les
   asigna puertos dinámicos. Para que el proxy de Vite pueda apuntar a
   puertos conocidos, el AppHost los fija explícitamente:

   ```csharp
   // infrastructure/OrderFlow.AppHost/Program.cs
   var products = builder
       .AddProject<Projects.Products_API>("products-api")
       .WithEndpoint("https", e => e.Port = 7200)
       // …

   var orders = builder.AddProject<Projects.Orders_API>("orders-api")
       .WithEndpoint("https", e => e.Port = 7153)
       // …
   ```

   Si cambias estos puertos, hay que actualizar también
   [web/vite.config.ts](../web/vite.config.ts).

2. **Dashboard de Aspire sin token** en desarrollo, mediante variable de
   entorno en el `launchSettings.json` del AppHost:

   ```json
   "DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS": "true"
   ```

   Sin esto, Aspire pide un token por URL cada vez que arrancas, lo que
   incomoda los lanzadores automáticos (p. ej. DevLauncher).

## Verificación end-to-end

1. `/` muestra KPIs no nulos → proxies OK a ambos servicios.
2. `/products` lista productos con paginación.
3. `/orders/new`: seleccionar 2 líneas, rellenar dirección, crear → redirige a `/orders/:id` con 201.
4. `/orders/:id`: líneas y total visibles, estado inicial `Pending`. `SagaTimeline` hace polling y avanza.
5. **Confirmar** → 204 y el estado pasa a `Confirmed`. El Saga llega al paso final.
6. **Cancelar** con motivo → 204 y estado `Cancelled`.
7. `/orders?status=Confirmed` filtra correctamente.
8. Apagar `Products.API` → al refrescar `/products` aparece el `ErrorBanner` limpio (sin pantalla blanca).

## Troubleshooting

### "Consultando servicios…" se queda colgado

El proxy de Vite no consigue alcanzar Orders.API o Products.API. Causas típicas:

- **Aspire está arrancando todavía** (migraciones + seeds tardan ~30 s). Espera o mira el dashboard.
- **Puertos no fijados**: comprueba que `Program.cs` del AppHost tiene los `.WithEndpoint("https", e => e.Port = 7153)` para Orders y `7200` para Products.
- **RabbitMQ no disponible**: si el bus no arranca, el servicio falla y no sirve HTTP. Ver Setup-Local.

### `MassTransit.ConfigurationException: License must be specified`

Se ha colado MassTransit 9.x (comercial). Fuerza en todos los `.csproj`:
```xml
<PackageReference Include="MassTransit" Version="8.5.2" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.5.2" />
<PackageReference Include="MassTransit.EntityFrameworkCore" Version="8.5.2" />
<PackageReference Include="MassTransit.Azure.ServiceBus.Core" Version="8.5.2" />
```
Luego `dotnet restore OrderFlow.slnx`.

### Dashboard de Aspire pide token

Añadir al `launchSettings.json` del AppHost:
```json
"DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS": "true"
```

### El puerto 7153 / 7200 está ocupado

Otra instancia del servicio corriendo. Mata el proceso o cambia el puerto en `Program.cs` del AppHost **y** en `web/vite.config.ts`.

## Fuera de alcance

- Tests (la demo es visual; podría añadirse Vitest + Testing Library más adelante).
- Despliegue / build de producción.
- Autenticación / login UI.
- Paso por el Gateway YARP (se evita por CORS restringido y rate limits).
