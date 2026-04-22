namespace Payments.API.Domain;

public enum PaymentStatus
{
    Pending   = 0,
    Processed = 1,
    Failed    = 2,
    Refunded  = 3
}
