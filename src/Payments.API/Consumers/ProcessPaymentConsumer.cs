using MassTransit;
using OrderFlow.Contracts.Commands;
using OrderFlow.Contracts.Events.Payments;
using Payments.API.Domain;
using Payments.API.Infrastructure;
using Payments.API.Services;

namespace Payments.API.Consumers;

/// <summary>
/// Consumer del comando ProcessPayment enviado por la Saga de Orders.
/// Cobra el importe, persiste el resultado y publica el evento de respuesta.
/// </summary>
public class ProcessPaymentConsumer : IConsumer<ProcessPayment>
{
    private readonly IPaymentGateway   _gateway;
    private readonly PaymentsDbContext _dbContext;
    private readonly ILogger<ProcessPaymentConsumer> _logger;

    public ProcessPaymentConsumer(
        IPaymentGateway                  gateway,
        PaymentsDbContext                dbContext,
        ILogger<ProcessPaymentConsumer>  logger)
    {
        _gateway   = gateway;
        _dbContext = dbContext;
        _logger    = logger;
    }

    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Processing payment {Amount} {Currency} for order {OrderId}",
            msg.Amount, msg.Currency, msg.OrderId);

        var payment = Payment.Create(msg.OrderId, msg.CustomerId, msg.Amount, msg.Currency);
        var result  = await _gateway.ProcessAsync(
            msg.OrderId, msg.Amount, msg.Currency, context.CancellationToken);

        if (result.IsSuccess)
        {
            payment.MarkAsProcessed(result.TransactionId!);
            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new PaymentProcessed
            {
                OrderId     = msg.OrderId,
                PaymentId   = payment.Id,
                CustomerId  = msg.CustomerId,
                Amount      = msg.Amount,
                Currency    = msg.Currency,
                ProcessedAt = DateTime.UtcNow
            }, context.CancellationToken);

            _logger.LogInformation(
                "Payment processed for order {OrderId}. TransactionId={TransactionId}",
                msg.OrderId, result.TransactionId);
        }
        else
        {
            payment.MarkAsFailed(result.FailureReason);
            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new PaymentFailed
            {
                OrderId = msg.OrderId,
                Reason  = result.FailureReason ?? "Unknown error"
            }, context.CancellationToken);

            _logger.LogWarning(
                "Payment failed for order {OrderId}. Reason={Reason}",
                msg.OrderId, result.FailureReason);
        }
    }
}
