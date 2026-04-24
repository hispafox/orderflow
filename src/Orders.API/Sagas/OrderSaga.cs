using MassTransit;
using OrderFlow.Contracts.Commands;
using OrderFlow.Contracts.Events.Orders;
using OrderFlow.Contracts.Events.Payments;
using OrderFlow.Contracts.Events.Products;

namespace Orders.API.Sagas;

/// <summary>
/// Saga de pedidos: coordina el flujo distribuido entre Orders, Products y Payments.
/// Patrón: Orchestration — esta Saga es el coordinador central.
///
/// Flujo feliz:
///   OrderCreated → ReserveStock → StockReserved → ProcessPayment
///   → PaymentProcessed → OrderConfirmed
///
/// Flujo de compensación:
///   PaymentFailed → ReleaseStock → StockReleased → OrderFailed
///
/// Flujo sin stock:
///   StockInsufficient → OrderFailed
/// </summary>
public class OrderSaga : MassTransitStateMachine<OrderSagaState>
{
    public State Pending           { get; private set; } = null!;
    public State PaymentProcessing { get; private set; } = null!;
    public State Compensating      { get; private set; } = null!;
    public State Completed         { get; private set; } = null!;
    public State Failed            { get; private set; } = null!;

    public Event<OrderCreated>      OrderCreatedEvent       { get; private set; } = null!;
    public Event<StockReserved>     StockReservedEvent      { get; private set; } = null!;
    public Event<StockInsufficient> StockInsufficientEvent  { get; private set; } = null!;
    public Event<PaymentProcessed>  PaymentProcessedEvent   { get; private set; } = null!;
    public Event<PaymentFailed>     PaymentFailedEvent      { get; private set; } = null!;
    public Event<StockReleased>     StockReleasedEvent      { get; private set; } = null!;

    // ReservationTimeout desactivado temporalmente para la demo: requiere un
    // MessageScheduler registrado (plugin rabbitmq_delayed_message_exchange
    // en el broker o scheduler externo tipo Quartz). Sin él, Schedule(...)
    // lanza PayloadNotFoundException y tumba la saga. Para rehabilitar ver
    // docs/demo/Inter-Service-Communications.md §6.3.
    // public Schedule<OrderSagaState, OrderSagaTimeout> ReservationTimeout { get; private set; } = null!;

    public OrderSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderCreatedEvent,      x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReservedEvent,     x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockInsufficientEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentProcessedEvent,  x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailedEvent,     x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReleasedEvent,     x => x.CorrelateById(ctx => ctx.Message.OrderId));

        // Schedule(() => ReservationTimeout, x => x.ReservationTimeoutTokenId, s =>
        // {
        //     s.Delay    = TimeSpan.FromMinutes(5);
        //     s.Received = x => x.CorrelateById(ctx => ctx.Message.OrderId);
        // });

        Initially(
            When(OrderCreatedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.CustomerId    = ctx.Message.CustomerId;
                    ctx.Saga.CustomerEmail = ctx.Message.CustomerEmail;
                    ctx.Saga.ProductId     = ctx.Message.Items.First().ProductId;
                    ctx.Saga.Quantity      = ctx.Message.Items.Sum(i => i.Quantity);
                    ctx.Saga.Amount        = ctx.Message.Total;
                    ctx.Saga.Currency      = ctx.Message.Currency;
                    ctx.Saga.CreatedAt     = DateTime.UtcNow;
                })
                .Publish(ctx => new ReserveStock
                {
                    OrderId   = ctx.Saga.CorrelationId,
                    ProductId = ctx.Saga.ProductId,
                    Quantity  = ctx.Saga.Quantity
                })
                // .Schedule(ReservationTimeout,
                //     ctx => new OrderSagaTimeout { OrderId = ctx.Saga.CorrelationId })
                .TransitionTo(Pending));

        During(Pending,
            When(StockReservedEvent)
                // .Unschedule(ReservationTimeout)
                .Publish(ctx => new ProcessPayment
                {
                    OrderId    = ctx.Saga.CorrelationId,
                    CustomerId = ctx.Saga.CustomerId,
                    Amount     = ctx.Saga.Amount,
                    Currency   = ctx.Saga.Currency
                })
                .TransitionTo(PaymentProcessing),

            When(StockInsufficientEvent)
                // .Unschedule(ReservationTimeout)
                .Then(ctx => ctx.Saga.FailureReason =
                    $"Insufficient stock. Requested: {ctx.Message.RequestedQuantity}, " +
                    $"Available: {ctx.Message.AvailableQuantity}")
                .Publish(ctx => new OrderFailed
                {
                    OrderId = ctx.Saga.CorrelationId,
                    Reason  = ctx.Saga.FailureReason!
                })
                .TransitionTo(Failed)
                .Finalize());

            // When(ReservationTimeout.Received)
            //     .Then(ctx => ctx.Saga.FailureReason =
            //         "Timeout: Products.API did not respond within 5 minutes")
            //     .Publish(ctx => new OrderFailed
            //     {
            //         OrderId = ctx.Saga.CorrelationId,
            //         Reason  = ctx.Saga.FailureReason!
            //     })
            //     .TransitionTo(Failed)
            //     .Finalize());

        During(PaymentProcessing,
            When(PaymentProcessedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.PaymentId   = ctx.Message.PaymentId;
                    ctx.Saga.CompletedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new OrderConfirmed
                {
                    OrderId       = ctx.Saga.CorrelationId,
                    CustomerId    = ctx.Saga.CustomerId,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    PaymentId     = ctx.Saga.PaymentId!.Value,
                    Total         = ctx.Saga.Amount,
                    Currency      = ctx.Saga.Currency,
                    ConfirmedAt   = DateTime.UtcNow
                })
                .TransitionTo(Completed)
                .Finalize(),

            When(PaymentFailedEvent)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .Publish(ctx => new ReleaseStock
                {
                    OrderId   = ctx.Saga.CorrelationId,
                    ProductId = ctx.Saga.ProductId,
                    Quantity  = ctx.Saga.Quantity
                })
                .TransitionTo(Compensating));

        During(Compensating,
            When(StockReleasedEvent)
                .Publish(ctx => new OrderFailed
                {
                    OrderId = ctx.Saga.CorrelationId,
                    Reason  = ctx.Saga.FailureReason!
                })
                .TransitionTo(Failed)
                .Finalize());

        SetCompletedWhenFinalized();
    }
}
