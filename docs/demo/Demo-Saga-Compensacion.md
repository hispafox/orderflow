# Demo · Saga, compensación y race conditions

Guion paso a paso para **demostrar en vivo** tres escenarios del Saga:

1. **Happy path** — pago OK, pedido confirmado.
2. **Compensación** — pago rechazado, reserva de stock liberada, pedido fallido.
3. **Race condition** — click manual en "Confirmar pedido" mientras la saga está corriendo (muestra por qué **no** mezclas UI manual con orquestación automática).

La demo está pensada para ~10 minutos de sesión en clase. Requiere que todo el stack esté arrancado (backend + SPA). Ver [Setup-Local.md](Setup-Local.md) para poner a punto el entorno.

---

## 0. Prerrequisitos

- RabbitMQ corriendo (http://localhost:15672 debe responder).
- `docs/demo/Demo-Frontend-React.md` aplicado: SPA en http://localhost:5173.
- AppHost de Aspire con todos los servicios en verde.
- El commit con el `OrderFailedProjector` presente (a partir de `3229a9c`).
- El commit con `Schedule()` desactivado en el saga (a partir de `3d23cd2`) — ver [Inter-Service-Communications.md](Inter-Service-Communications.md) §12.

Abrir tres pestañas del navegador (recomendado):

| Pestaña | URL | Para qué |
|---|---|---|
| SPA | http://localhost:5173/orders | Ver cambios de estado del pedido |
| Eventos | http://localhost:5173/events | Ver mensajes del Outbox aparecer |
| RabbitMQ | http://localhost:15672/#/queues | Ver colas y tasas de publish/ack |

---

## 1. El único cambio de código necesario

Toda la demo gira alrededor del **gateway de pagos fake** de `Payments.API`.
Fichero: [src/Payments.API/Services/FakePaymentGateway.cs](../../src/Payments.API/Services/FakePaymentGateway.cs).

Por defecto usa un random 90/10:
```csharp
var success = _random.NextDouble() > 0.1;  // 90% éxito, 10% fallo
```

Para tener **control determinista** durante la clase, se sustituye esa línea por una constante:

```csharp
// Forzar siempre éxito (happy path)
var success = true;

// O forzar siempre fallo (compensación)
var success = false;
```

Ejemplo con ambas versiones comentadas para alternar rápido:

```csharp
public async Task<PaymentResult> ProcessAsync(
    Guid orderId, decimal amount, string currency, CancellationToken ct = default)
{
    await Task.Delay(TimeSpan.FromMilliseconds(200), ct);

    // --- DEMO · DESCOMENTA UNA SOLA LÍNEA ---
    // var success = _random.NextDouble() > 0.1;   // A · aleatorio (default producción)
    // var success = true;                          // B · siempre OK (happy path)
    var success = false;                            // C · siempre fallo (compensación)

    if (success)
    {
        var transactionId = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "[PAGO SIMULADO] OrderId={OrderId} Amount={Amount} {Currency} TransactionId={TransactionId}",
            orderId, amount, currency, transactionId);
        return PaymentResult.Succeeded(transactionId, orderId, amount, currency);
    }

    var reason = "Insufficient funds (simulated)";
    _logger.LogWarning(
        "[PAGO SIMULADO FALLIDO] OrderId={OrderId} Amount={Amount} {Currency} Reason={Reason}",
        orderId, amount, currency, reason);
    return PaymentResult.Failed(orderId, reason);
}
```

> **IMPORTANTE:** cambiar un `.cs` **no se aplica en caliente**. Tras editar:
> 1. Guarda el fichero.
> 2. En el dashboard de Aspire (http://localhost:17149) → resource `payments-api` → **Stop**.
> 3. Espera a que aparezca en rojo.
> 4. **Start** otra vez.
>
> Sin reiniciar, el proceso corre el binario antiguo — efecto habitual: "le cambio el código y sigue pasando lo mismo". Ver también [Inter-Service-Communications.md](Inter-Service-Communications.md) §7 para el detalle del flujo.

---

## 2. Escenario A · Happy path (`success = true`)

### Guion

1. Ajusta `FakePaymentGateway.cs` → `var success = true;`.
2. Stop/Start `payments-api` en Aspire.
3. Refresca las 3 pestañas con Ctrl+F5.
4. En la SPA: **Nuevo pedido** → 2 líneas → crear.

### Timeline esperada

| t (aprox) | Qué pasa | Qué ves en las 3 pestañas |
|---|---|---|
| **0 s** | `POST /api/orders` 201 → redirige a `/orders/:id` | SPA: badge **Pendiente**. Panel saga: "Saga aún no iniciado". |
| **~1 s** | Outbox publica `OrderCreated` (QueryDelay=1s) | Eventos: aparece `OrderCreated`. RabbitMQ: cola `OrderCreated` con +1 publish. |
| **~1-2 s** | Saga consume `OrderCreated` → fila `OrderSagaState`=`Pending` + publica `ReserveStock` | SPA saga: estado **Pending**, paso "Reserva de stock" en curso. Eventos: `ReserveStock`. |
| **~2-3 s** | Products reserva stock → publica `StockReserved` → saga → publica `ProcessPayment` → estado `AwaitingPayment` | SPA saga: stock ✅, pago "en curso". |
| **~3-4 s** | Payments cobra OK → publica `PaymentProcessed` → saga publica `OrderConfirmed` → `Finalize()` | SPA: badge pasa a **Confirmado**. |
| **~4-5 s** | `OrderConfirmedProjector` (Orders) y `OrderConfirmedConsumer` (Notifications) consumen | OrderSummary=Confirmed, log de Notifications "email enviado". |

### Qué señalar en clase

- La duración **total** del flujo (4-5 s) es la latencia de un saga distribuido con 5 servicios + BD + broker.
- `OrderSagaState` existe durante el flujo y **desaparece** al finalizar (`SetCompletedWhenFinalized`).
- Los mensajes aparecen y desaparecen rápido del Outbox: el `QueryDelay=1s` los publica casi al instante.

---

## 3. Escenario B · Compensación (`success = false`)

### Guion

1. Ajusta `FakePaymentGateway.cs` → `var success = false;`.
2. Stop/Start `payments-api`.
3. Antes de crear el pedido, abre la pestaña **Productos** y anota el stock actual del producto que vas a usar — **lo necesitas para el final**.
4. Nuevo pedido → una línea con ese producto, cantidad 2 → crear.

### Timeline esperada

| t | Qué pasa | Qué ves |
|---|---|---|
| **0 s** | POST crea el pedido | Badge **Pendiente**. |
| **~1 s** | Outbox publica `OrderCreated` | Eventos: `OrderCreated`. |
| **~2 s** | Saga → `ReserveStock` → Products **descuenta stock real** → `StockReserved` | Panel Productos: stock bajó en 2 unidades. Eventos: varios. |
| **~2-3 s** | Saga → `ProcessPayment` → `FakePaymentGateway` devuelve Fail → `PaymentFailed` | Log warning "[PAGO SIMULADO FALLIDO]" en Payments.API. |
| **~3-4 s** | Saga entra en `Compensating` → publica `ReleaseStock` → Products **repone el stock** → publica `StockReleased` | Panel Productos: stock vuelve al valor inicial. |
| **~4-5 s** | Saga publica `OrderFailed` → Finalize. `OrderFailedProjector` → `OrderSummary.Status="Failed"`. | SPA: badge pasa a **Fallido** con motivo "Insufficient funds (simulated)". |

### Qué señalar en clase

- **La compensación es real, no simbólica**: el stock se descontó en Products.API y luego se repuso. Mira la tabla `[products].[Products]` si dudas (o simplemente la pestaña `/products` de la SPA antes y después).
- El pago también está persistido: `[payments].[Payments]` tiene una fila con `Status=Failed` y `FailureReason='Insufficient funds (simulated)'`.
- Los eventos aparecen en [Eventos](../web/src/pages/EventsPage.tsx) en secuencia: `OrderCreated` → `ReserveStock` → `ProcessPayment` → `ReleaseStock` → `OrderFailed`.
- En la UI de RabbitMQ (`queues`) se ve cómo se procesa cada comando — útil para señalar la naturaleza asíncrona del flujo.

---

## 4. Escenario C · Race condition (`success = false` + click Confirmar)

### Guion

Misma configuración que el escenario B (`success = false`).

1. Nuevo pedido → redirige a `/orders/:id`.
2. **¡Rápido!** Antes de que pasen ~3 segundos, click en **"Confirmar pedido"** (botón azul, aparece mientras el estado es Pending).
3. Observa cómo termina.

### Qué pasa por dentro

```
t=0s   POST /api/orders → Order.Status=Pending (write side) + OrderSummary.Status=Pending (read)
t=1s   Tu click: POST /api/orders/{id}/confirm
       → ConfirmOrderHandler carga Order y llama order.Confirm()
       → Order.Status=Confirmed (write side)
       ⚠ NO publica OrderConfirmed al bus (eso sólo lo hace la saga)
t=1-3s Saga sigue su curso (no se entera del click manual)
t=3s   Payments Fail → Compensating → ReleaseStock → StockReleased
t=4s   Saga publica OrderFailed → Finalize
t=4s   OrderFailedProjector → OrderSummary.Status=Failed
```

### Estado final (inconsistente)

| Modelo | Status |
|---|---|
| **Write side** (`[orders].[Orders].Status`) | `Confirmed` (lo que hiciste tú) |
| **Read model** (`[orders].[OrderSummaries].Status`) | `Failed` (lo que puso la saga) |
| **SPA** (lee del read model vía `GetOrderByIdQuery`) | **Fallido** → la saga gana visualmente |

### Qué señalar en clase

- Este es un **anti-patrón típico** al mezclar UI manual con saga automática: **split-brain** entre modelos.
- La causa raíz: `ConfirmOrderHandler` cambia el write side pero **no coordina con la saga**. La saga ignora que el usuario ya confirmó.
- **Cómo se evita en producción:**
  - Deshabilitar el botón mientras haya una saga activa (query `saga-state` y solo permitir si no existe y el Order lleva > X segundos sin moverse).
  - Hacer que `ConfirmOrderCommand` publique un `ConfirmByAdmin` consumido por la saga, que decida si puede honrarlo o no.
  - Restringir el endpoint a un rol `admin` y aceptar la inconsistencia como decisión de negocio.

---

## 5. Volver al estado "normal"

Al terminar la clase, recomponer:

1. `FakePaymentGateway.cs` → descomentar la línea aleatoria original:
   ```csharp
   var success = _random.NextDouble() > 0.1;
   ```
2. Stop/Start `payments-api`.
3. Si quedaron pedidos zombi en la BD, puedes dejarlos (no molestan) o dropear las BDs con el comando de [Setup-Local.md](Setup-Local.md) §5.

---

## 6. Tabla cheatsheet de "¿dónde miro qué?"

| Qué quieres ver | Dónde | Observación |
|---|---|---|
| Estado del saga en tiempo real | SPA `/orders/:id` → panel "Estado del Saga" | Polling 2s; cuando el saga termina, reconstrucción post-mortem desde el Order |
| Eventos publicados por Orders | SPA `/events` | Outbox de Orders.API (no incluye eventos de Products/Payments) |
| Todas las colas y tasas | http://localhost:15672/#/queues (guest/guest) | Vista global absoluta del broker |
| Estado del stock | SPA `/products` | Para ver que la compensación repuso unidades |
| Pagos persistidos | SSMS a `(localdb)\MSSQLLocalDB` → BD `OrderFlowPayments` → `[payments].[Payments]` | Filas reales con Status + FailureReason |
| Logs por servicio | Dashboard Aspire → resource → **Consola** | Más fiable que "Registros estructurados" en arranque |
| Trazas OTel | Dashboard Aspire → **Seguimientos** | Ver una petición end-to-end cruzando servicios |

---

## 7. Problemas frecuentes durante la demo

| Síntoma | Causa | Solución |
|---|---|---|
| El pedido se queda en Pending para siempre | `payments-api` no se reinició tras editar el `.cs` | Stop + Start en el dashboard |
| "Saga aún no iniciado" tras 10 s | Falta el commit `3d23cd2` (Schedule desactivado) → saga falla con PayloadNotFoundException | `git pull origin main` + restart `orders-api` |
| SPA muestra "Confirmed" al crear sin esperar | Estás mirando un pedido viejo (pre-fix) | Crea uno nuevo desde la pestaña |
| Panel Outbox siempre vacío | Los mensajes se cleanean en ~1s tras publicarse | Normal — abre la pestaña Eventos **antes** de crear el pedido para pillarlos |
| Payments emite "TransactionId" aunque forzaste fallo | Binario viejo cargado | Stop/Start `payments-api` y mira la hora del último reinicio en Aspire |
| Reiniciar no coge el cambio | Aspire cachea el assembly | Ctrl+C en la terminal del AppHost y relanza, o fuerza `dotnet build` antes |

---

## 8. Scripts prácticos (copy/paste)

### Forzar fallo (PowerShell admin opcional)

```powershell
# Edita el fichero con VS Code (abre la línea directa)
code --goto "c:\w\repos\orderflow\src\Payments.API\Services\FakePaymentGateway.cs:32"
```

### Verificar stock de un producto vía Scalar

```
https://localhost:7200/scalar/v1
→ GET /api/products  → copia el id de uno
→ GET /api/products/{id} → anota el stock antes/después
```

### Inspeccionar colas vía CLI de RabbitMQ

```powershell
& "C:\Program Files\RabbitMQ Server\rabbitmq_server-*\sbin\rabbitmqctl.bat" list_queues name messages consumers
```
