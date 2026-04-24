# Comunicaciones entre servicios · OrderFlow

Documento de referencia que describe **cómo se comunican** los microservicios de OrderFlow entre sí: qué contratos usan, qué transporte, qué patrones y cómo se orquesta el flujo distribuido del pedido.

Sirve tanto de mapa mental para nuevos integrantes como de documento de revisión cuando algo se comporta de forma inesperada.

---

## Índice

1. [Vista general y principios](#1-vista-general-y-principios)
2. [Estilos de comunicación](#2-estilos-de-comunicación)
3. [Topología de servicios](#3-topología-de-servicios)
4. [Contratos compartidos](#4-contratos-compartidos-orderflowcontracts)
5. [Happy path del pedido](#5-happy-path-del-pedido)
6. [Flujos de compensación y fallo](#6-flujos-de-compensación-y-fallo)
7. [Máquina de estados del Saga](#7-máquina-de-estados-del-saga)
8. [Outbox pattern](#8-outbox-pattern-orders)
9. [Read model y proyectores](#9-read-model-y-proyectores)
10. [Correlation ID y trazabilidad](#10-correlation-id-y-trazabilidad)
11. [Resiliencia HTTP con Polly](#11-resiliencia-http-con-polly)
12. [Transport switch (RabbitMQ vs InMemory)](#12-transport-switch-rabbitmq-vs-inmemory)
13. [Glosario rápido](#13-glosario-rápido)

---

## 1. Vista general y principios

OrderFlow es un sistema **orientado a eventos** con 5 microservicios que cooperan para gestionar el ciclo de vida de un pedido. Los principios que guían sus comunicaciones:

| Principio | Qué significa aquí |
|---|---|
| **Database per Service** | Cada servicio tiene su propia BD en LocalDB. Ningún servicio toca tablas de otro. |
| **Async by default** | La integración entre servicios es vía eventos MassTransit + RabbitMQ. El HTTP síncrono se usa solo cuando es estrictamente necesario. |
| **Saga como coordinador** | `OrderSaga` (en Orders.API) orquesta el pedido distribuido. El resto de servicios son reactivos: consumen comandos/eventos y responden. |
| **Outbox transaccional** | Orders.API nunca publica eventos directamente; los escribe en una tabla Outbox dentro de la misma transacción EF y MassTransit los publica de forma asíncrona. |
| **Read model separado** | El listado de pedidos y el panel de KPIs leen de una proyección (`OrderSummaries`) actualizada por consumers — no del agregado de escritura. |
| **Correlation ID end-to-end** | Cada petición HTTP inicial genera un `X-Correlation-Id` que se propaga por todos los consumers y logs. |

---

## 2. Estilos de comunicación

Conviven dos estilos, deliberadamente:

```mermaid
flowchart LR
  subgraph "HTTP síncrono (cuando hace falta un dato YA)"
    A[Orders.API] -->|GET /api/products/:id<br/>ProductsClient| B[Products.API]
  end

  subgraph "Mensajería asíncrona (flujo de negocio)"
    C[Orders.API] -.publish.-> Q((RabbitMQ))
    Q -.consume.-> D[Products.API]
    Q -.consume.-> E[Payments.API]
    Q -.consume.-> F[Notifications.API]
  end
```

**HTTP síncrono** — se reserva para consultas puntuales que no pueden esperar (ej. Orders necesita saber el nombre actualizado de un producto antes de persistir la línea del pedido). Protegido con Polly (retry + circuit breaker + timeout + fallback, ver sección 11).

**Mensajería asíncrona** — usado para todo el flujo de negocio. Más resiliente, más desacoplado, más fácil de escalar. El coste es latencia (eventual consistency) y complejidad operativa (colas, DLQ, idempotencia).

---

## 3. Topología de servicios

```mermaid
flowchart TB
  subgraph Cliente
    SPA[SPA Demo<br/>web/ React+Vite]
  end

  subgraph Backend
    GW[Gateway.API<br/>YARP + JWT]
    ORD[Orders.API<br/>Controllers + Saga]
    PRD[Products.API<br/>Minimal APIs]
    PAY[Payments.API<br/>Consumers only]
    NOT[Notifications.API<br/>Consumers + Fake Email]
  end

  subgraph Infra
    SQL[(SQL Server<br/>LocalDB · una BD/servicio)]
    MQ([RabbitMQ<br/>:5672])
  end

  SPA -->|"/api/orders/*"<br/>Vite proxy 7153| ORD
  SPA -->|"/api/products/*"<br/>Vite proxy 7200| PRD
  SPA -.futuro.-> GW

  ORD -->|HTTP sync<br/>ProductsClient| PRD

  ORD <-->|publish + consume| MQ
  PRD <-->|publish + consume| MQ
  PAY <-->|consume + publish| MQ
  NOT <-->|consume only| MQ

  ORD --- SQL
  PRD --- SQL
  PAY --- SQL
  NOT --- SQL
  GW  --- SQL
```

| Servicio | Responsabilidad | Publica | Consume |
|---|---|---|---|
| **Orders.API** | Persiste el pedido, ejecuta la Saga, expone CRUD HTTP | `OrderCreated`, `OrderConfirmed`, `OrderCancelled`, `OrderFailed`, `ReserveStock`, `ProcessPayment`, `ReleaseStock` | `OrderCreated` (proyector + saga), `StockReserved`, `StockInsufficient`, `StockReleased`, `PaymentProcessed`, `PaymentFailed`, `ProductUpdated` |
| **Products.API** | Catálogo + stock, expone HTTP, reacciona a comandos | `StockReserved`, `StockInsufficient`, `StockReleased`, `ProductUpdated` | `ReserveStock`, `ReleaseStock` |
| **Payments.API** | Simula cobro. **Sin HTTP público.** | `PaymentProcessed`, `PaymentFailed` | `ProcessPayment` |
| **Notifications.API** | Envía emails "fake". **Sin HTTP público.** | — | `OrderCreated`, `OrderConfirmed`, `OrderFailed` |
| **Gateway.API** | YARP + JWT + rate limit. **La demo SPA lo esquiva.** | — | — |

> En la demo actual, el frontend llama directamente a `Orders.API` (7153) y `Products.API` (7200) a través del proxy de Vite con `secure:false`. Se salta el Gateway porque exige login y tiene CORS restrictivo.

---

## 4. Contratos compartidos (`OrderFlow.Contracts`)

Los tipos de los mensajes viven en un único proyecto compartido, referenciado por todos los servicios. Esto garantiza que publishers y consumers hablen el mismo "idioma" sin acoplarse a la implementación.

```
shared/OrderFlow.Contracts/
├── Commands/             ← mensajes de intención (imperativo)
│   ├── ProcessPayment.cs
│   ├── ReserveStock.cs
│   └── ReleaseStock.cs
└── Events/               ← hechos ocurridos (pasado)
    ├── Orders/
    │   ├── OrderCreated.cs
    │   ├── OrderConfirmed.cs
    │   ├── OrderCancelled.cs
    │   └── OrderFailed.cs
    ├── Products/
    │   ├── StockReserved.cs
    │   └── ProductUpdated.cs
    └── Payments/
        └── PaymentProcessed.cs
```

Convención:

- **Commands** → verbo en imperativo (`ReserveStock`, `ProcessPayment`). El publisher le pide a un servicio concreto que haga algo. *Un único consumer lógico por comando.*
- **Events** → verbo en pasado (`OrderCreated`, `PaymentProcessed`). Describe algo que ya pasó. *Varios consumers posibles.*

---

## 5. Happy path del pedido

Secuencia completa sin fallos:

```mermaid
sequenceDiagram
    autonumber
    participant C as SPA Cliente
    participant O as Orders.API
    participant S as OrderSaga<br/>(en Orders.API)
    participant MQ as RabbitMQ
    participant P as Products.API
    participant Pay as Payments.API
    participant N as Notifications.API

    C->>O: POST /api/orders
    O->>O: Persiste Order + Outbox<br/>(misma transacción)
    O-->>C: 201 Created · OrderDto
    O->>MQ: publish OrderCreated<br/>(desde outbox, ~1s delay)

    MQ->>S: OrderCreated
    S->>S: crea OrderSagaState<br/>State = Pending
    S->>MQ: publish ReserveStock
    S->>MQ: schedule ReservationTimeout (5 min)

    MQ->>N: OrderCreated
    N->>N: enviar email "pedido recibido" (fake)

    MQ->>P: ReserveStock
    P->>P: descuenta stock (tx)
    P->>MQ: publish StockReserved

    MQ->>S: StockReserved
    S->>S: State = PaymentProcessing
    S->>S: unschedule ReservationTimeout
    S->>MQ: publish ProcessPayment

    MQ->>Pay: ProcessPayment
    Pay->>Pay: simula cobro (tx)
    Pay->>MQ: publish PaymentProcessed

    MQ->>S: PaymentProcessed
    S->>S: State = Completed<br/>Finalize()
    S->>MQ: publish OrderConfirmed

    MQ->>N: OrderConfirmed
    N->>N: enviar email "pedido confirmado"

    MQ->>O: OrderConfirmed<br/>(OrderConfirmedProjector)
    O->>O: OrderSummary.Status = Confirmed
```

**Duración típica end-to-end:** 200–800 ms con RabbitMQ en local (incluye el delay del outbox).

**Sincronías importantes:**

- El `201 Created` de la API (paso 3) se devuelve **antes** de que el saga arranque. El cliente no espera al resultado del saga — por eso la SPA muestra `Pending` inmediatamente y hace polling de `saga-state`.
- El `OrderConfirmed` viaja **dos veces** desde el saga: una al servicio de notificaciones (efecto lateral) y otra al propio Orders.API (proyector que actualiza el read model). Ambos consumers son independientes.

---

## 6. Flujos de compensación y fallo

El Saga implementa dos ramas de recuperación cuando algo no va bien:

### 6.1 Stock insuficiente (sin compensación)

```mermaid
sequenceDiagram
    autonumber
    participant S as Saga
    participant MQ as RabbitMQ
    participant P as Products.API
    participant N as Notifications.API

    S->>MQ: ReserveStock
    MQ->>P: ReserveStock
    P->>P: stock &lt; requested
    P->>MQ: publish StockInsufficient
    MQ->>S: StockInsufficient
    S->>S: State = Failed<br/>Finalize()
    S->>MQ: publish OrderFailed
    MQ->>N: OrderFailed → email "pedido rechazado"
```

No hace falta compensar: el stock nunca se descontó.

### 6.2 Pago fallido (con compensación)

```mermaid
sequenceDiagram
    autonumber
    participant S as Saga
    participant MQ as RabbitMQ
    participant P as Products.API
    participant Pay as Payments.API
    participant N as Notifications.API

    Note over S: llegó StockReserved<br/>State = PaymentProcessing

    S->>MQ: ProcessPayment
    MQ->>Pay: ProcessPayment
    Pay->>Pay: cobro rechazado
    Pay->>MQ: publish PaymentFailed

    MQ->>S: PaymentFailed
    S->>S: State = Compensating
    S->>MQ: publish ReleaseStock

    MQ->>P: ReleaseStock
    P->>P: repone stock (tx)
    P->>MQ: publish StockReleased

    MQ->>S: StockReleased
    S->>S: State = Failed<br/>Finalize()
    S->>MQ: publish OrderFailed
    MQ->>N: OrderFailed → email "pedido rechazado"
```

El Saga libera el stock reservado **antes** de marcar el pedido como fallido, preservando consistencia eventual.

### 6.3 Timeout de reserva

Si `Products.API` no responde en 5 minutos (ni con `StockReserved` ni con `StockInsufficient`), el Saga dispara el `ReservationTimeout` programado y considera el pedido fallido.

```csharp
Schedule(() => ReservationTimeout, x => x.ReservationTimeoutTokenId, s =>
{
    s.Delay    = TimeSpan.FromMinutes(5);
    s.Received = x => x.CorrelateById(ctx => ctx.Message.OrderId);
});
```

> **Importante:** `Schedule(...)` requiere un `MessageScheduler` registrado en el bus. Con RabbitMQ usamos el scheduler in-process. En modo **InMemory** hay que añadir `cfg.UseDelayedMessageScheduler()` o el saga falla con `PayloadNotFoundException: MessageSchedulerContext`. Por eso la demo requiere RabbitMQ.

---

## 7. Máquina de estados del Saga

Visualización del `OrderSaga` de [src/Orders.API/Sagas/OrderSaga.cs](../../src/Orders.API/Sagas/OrderSaga.cs):

```mermaid
stateDiagram-v2
    [*] --> Pending: OrderCreated<br/>publish ReserveStock<br/>schedule ReservationTimeout

    Pending --> PaymentProcessing: StockReserved<br/>unschedule timeout<br/>publish ProcessPayment
    Pending --> Failed: StockInsufficient<br/>publish OrderFailed
    Pending --> Failed: ReservationTimeout<br/>publish OrderFailed

    PaymentProcessing --> Completed: PaymentProcessed<br/>publish OrderConfirmed
    PaymentProcessing --> Compensating: PaymentFailed<br/>publish ReleaseStock

    Compensating --> Failed: StockReleased<br/>publish OrderFailed

    Completed --> [*]: Finalize()
    Failed --> [*]: Finalize()
```

- `Finalize()` + `SetCompletedWhenFinalized()` → al llegar a un estado terminal, MassTransit borra la fila de `OrderSagaState`. Por eso el endpoint `GET /api/orders/{id}/saga-state` devuelve **404 cuando el pedido ya terminó** (confirmado, cancelado o fallido).
- La SPA interpreta el 404 como "terminó" y deja de hacer polling.

---

## 8. Outbox pattern (Orders)

Orders.API nunca publica a RabbitMQ directamente desde el handler. Todo pasa por la tabla **Outbox** que MassTransit gestiona:

```mermaid
sequenceDiagram
    autonumber
    participant H as Command Handler
    participant DB as OrderDbContext
    participant OB as Tabla Outbox<br/>(misma BD)
    participant MT as BusOutbox<br/>(MassTransit)
    participant MQ as RabbitMQ

    H->>DB: SaveChanges<br/>(Order + Outbox row)
    Note over DB: transacción ACID única<br/>— o se guarda todo o nada
    H-->>H: return OrderDto

    loop cada 1s
        MT->>OB: SELECT TOP 1 WITH (UPDLOCK, ROWLOCK, READPAST)
        OB-->>MT: mensaje pendiente
        MT->>MQ: publish
        MT->>OB: marcar Delivered
    end
```

**Beneficios:**

- La publicación del evento es **consistente con el estado** del agregado. Si el commit falla, el mensaje nunca sale.
- Reintentos automáticos si RabbitMQ está temporalmente caído — MassTransit vuelve a leer la outbox.
- Se limpia sólo (`OutboxCleanupJob`) para no inflar la tabla.

Configurado en [src/Orders.API/Program.cs](../../src/Orders.API/Program.cs):
```csharp
x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
{
    o.UseSqlServer();
    o.QueryDelay        = TimeSpan.FromSeconds(1);
    o.QueryMessageLimit = 100;
    o.UseBusOutbox(bo => bo.MessageDeliveryTimeout = TimeSpan.FromMinutes(5));
});
```

---

## 9. Read model y proyectores

El listado de pedidos, los KPIs y todas las lecturas "rápidas" van contra una tabla **`OrderSummaries`** (una fila por pedido) separada del agregado de escritura `Orders`. Tres proyectores MassTransit la mantienen sincronizada:

| Proyector | Evento | Qué hace |
|---|---|---|
| `OrderSummaryProjector` | `OrderCreated` | Inserta la fila inicial con `Status = Pending` |
| `OrderConfirmedProjector` | `OrderConfirmed` | `Status = Confirmed`, `ConfirmedAt = now` |
| `OrderCancelledProjector` | `OrderCancelled` | `Status = Cancelled`, `CancellationReason = …` |

```mermaid
flowchart LR
  Saga --> MQ
  MQ --> Proj1[OrderSummaryProjector]
  MQ --> Proj2[OrderConfirmedProjector]
  MQ --> Proj3[OrderCancelledProjector]
  Proj1 --> RM[(OrderSummaries)]
  Proj2 --> RM
  Proj3 --> RM

  API[GET /api/orders] --> RM
  API2[GET /api/orders/:id] --> W[(Orders<br/>write side)]
```

**Consecuencia operativa:** si un proyector falla o se desregistra, el listado queda **desfasado** respecto al detalle. Síntoma clásico: en `/orders` aparece `Pending` pero el detalle dice `Confirmed`. Causas típicas:
- Proyector no registrado en `AddMassTransit` (ver commit `ae49022`).
- Transport en InMemory → los eventos se quedan en el proceso que los publicó.
- Consumer con excepción repetida → el evento va a la DLQ.

---

## 10. Correlation ID y trazabilidad

Cada petición HTTP genera un `X-Correlation-Id` (GUID v4). El middleware `CorrelationIdMiddleware` de cada servicio:

1. Lee el header si viene del cliente (la SPA lo inyecta en [`web/src/api/client.ts`](../../web/src/api/client.ts)).
2. Si no, genera uno nuevo.
3. Lo inyecta en el `LogContext` de Serilog → aparece en todos los logs de la petición.
4. Lo añade al header de respuesta → el cliente lo puede usar para soporte.
5. `CorrelationIdDelegatingHandler` lo propaga a las llamadas HTTP salientes (Orders → Products).
6. En los eventos MassTransit, el correlation ID **no se transmite explícitamente** en el body; en su lugar usamos el `CorrelationId` nativo del `ConsumeContext` (GUID único por mensaje) + el `TraceId` de OpenTelemetry.

```mermaid
flowchart LR
    Browser -->|X-Correlation-Id: abc| Ord
    Ord -->|X-Correlation-Id: abc| Prod
    Ord -.OrderCreated<br/>CorrelationId=abc.-> MQ
    MQ -.-> Proj & Saga
    Serilog[("Logs<br/>CorrelationId=abc")]
    Ord --> Serilog
    Prod --> Serilog
    Saga --> Serilog
```

Esto permite buscar una única petición en todos los logs estructurados por `CorrelationId: abc`.

---

## 11. Resiliencia HTTP con Polly

La única llamada HTTP síncrona entre servicios es **Orders.API → Products.API** (validar nombre del producto al crear una línea). Como cualquier RPC remoto puede fallar, se protege con una pipeline Polly:

```mermaid
flowchart TB
    Call[Orders Handler] --> FB[Fallback<br/>503 si todo falla]
    FB --> CB[Circuit Breaker<br/>50% fallos / 30s]
    CB --> RT[Retry<br/>3 intentos<br/>expo + jitter]
    RT --> TO[Timeout<br/>5s / 10s]
    TO --> HTTP[Products.API]
```

Configurada en [src/Orders.API/Program.cs](../../src/Orders.API/Program.cs) con `AddResilienceHandler("products-pipeline")`. Orden, de dentro a fuera: **Timeout → Retry → CircuitBreaker → Fallback**. Cuando el circuito está abierto o el timeout se consume, el fallback devuelve un `503 ServiceUnavailable` falso en lugar de una excepción, para que el handler decida cómo tratar la degradación.

---

## 12. Transport switch (RabbitMQ vs InMemory)

Cada servicio lee `Messaging:Transport` de su `appsettings.Development.json`:

```json
{ "Messaging": { "Transport": "RabbitMQ" } }
```

| Valor | Cuándo usar | Limitaciones |
|---|---|---|
| `"RabbitMQ"` (default recomendado) | Demo completa, flujos cross-service, pruebas end-to-end | Requiere RabbitMQ operativo en `amqp://guest:guest@localhost:5672` |
| `"InMemory"` | Desarrollo aislado de **un solo servicio** sin broker | **No cruza procesos** → el Saga no puede orquestar. Si el Saga usa `Schedule(...)`, falla con `PayloadNotFoundException: MessageSchedulerContext` a menos que se registre un `DelayedMessageScheduler` |

Ver también [messaging-transport-switch.md](messaging-transport-switch.md) para los detalles del switch.

**Regla de oro para esta demo:** usa `RabbitMQ`. El Saga depende de él.

---

## 13. Glosario rápido

| Término | Definición |
|---|---|
| **Command** | Mensaje imperativo (hacer algo). Uno-a-uno entre publisher y consumer. Ej: `ReserveStock`. |
| **Event** | Mensaje declarativo (ya pasó). Publish/subscribe, múltiples consumers. Ej: `OrderCreated`. |
| **Saga** | Máquina de estados distribuida que coordina una transacción de negocio cruzando varios servicios. |
| **Outbox** | Tabla en la BD del servicio donde se escriben los eventos a publicar, en la misma transacción que el cambio de estado, para garantizar consistencia. |
| **Projector** | Consumer que traduce eventos a una vista de lectura (read model), manteniéndola sincronizada asincrónicamente. |
| **Correlation ID** | Identificador que viaja con una petición a través de todos los servicios, útil para debugging y observabilidad. |
| **Circuit Breaker** | Patrón Polly que abre el circuito cuando un dependiente falla consistentemente, evitando saturarlo con llamadas condenadas a fallar. |
| **Compensation** | Acción que revierte un efecto lateral ya aplicado en otro servicio (ej. `ReleaseStock` compensa `ReserveStock`). |

---

## Referencias cruzadas

- [docs/Setup-Local.md](Setup-Local.md) — instalación del entorno (RabbitMQ, LocalDB, etc.).
- [docs/messaging-transport-switch.md](messaging-transport-switch.md) — switch InMemory/RabbitMQ.
- [docs/M4.2-Mensajeria-MassTransit-Gamma.md](../M4.2-Mensajeria-MassTransit-Gamma.md) — módulo del curso sobre MassTransit.
- [docs/M4.3-Saga-Pattern-Gamma.md](../M4.3-Saga-Pattern-Gamma.md) — módulo del curso sobre el Saga Pattern.
- [docs/M6.2-Outbox-Pattern-Gamma.md](../M6.2-Outbox-Pattern-Gamma.md) — módulo del curso sobre Outbox.
- [docs/Demo-Frontend-React.md](Demo-Frontend-React.md) — cómo la SPA demo consume esta API.
