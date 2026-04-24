# OrderFlow — Demo Frontend

SPA de demostración (React + Vite + TypeScript) para visualizar el flujo distribuido de OrderFlow: catálogo de productos, creación de pedidos y orquestación del Saga entre `Orders.API`, `Products.API` y `Payments.API`.

> Es material extra del proyecto, no un módulo numerado del curso. No hay autenticación — los endpoints consumidos están en modo `AllowAnonymous`.

## Prerrequisitos

- Node.js 20+
- Backend OrderFlow arrancado vía Aspire AppHost (`dotnet run --project infrastructure/OrderFlow.AppHost`)
- Servicios escuchando en:
  - `https://localhost:7153` → `Orders.API`
  - `https://localhost:7200` → `Products.API`

## Arranque

```bash
cd web
npm install    # solo la primera vez
npm run dev
```

La SPA queda en `http://localhost:5173`.

Vite hace proxy a los dos servicios backend con `secure: false` para aceptar el dev cert de .NET. La app sólo necesita rutas relativas (`/api/orders`, `/api/products`), no tiene URLs absolutas.

## Scripts

| Script | Descripción |
|---|---|
| `npm run dev` | Servidor de desarrollo con HMR |
| `npm run build` | Type-check + build de producción |
| `npm run preview` | Sirve el build para inspección |
| `npm run typecheck` | Solo `tsc --noEmit` |

## Estructura

```
web/
├── vite.config.ts           ← proxy → Orders.API / Products.API
├── src/
│   ├── api/                 ← cliente HTTP + tipos TS espejo de los DTOs
│   ├── components/          ← StatusBadge, SagaTimeline, Money, Spinner, ErrorBanner
│   ├── pages/               ← Home, Products, Orders, OrderDetail, CreateOrder
│   ├── App.tsx              ← layout + navegación
│   └── main.tsx             ← bootstrap: Router + QueryClient
```

## Flujo típico de demo

1. `/products` — revisar catálogo y stock.
2. `/orders/new` — crear pedido seleccionando líneas.
3. `/orders/:id` — ver detalle + **panel del Saga** con polling (2 s) hasta estado terminal.
4. Confirmar o cancelar desde la misma vista y observar el estado actualizarse.
