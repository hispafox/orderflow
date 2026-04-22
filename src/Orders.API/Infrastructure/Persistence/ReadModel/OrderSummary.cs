namespace Orders.API.Infrastructure.Persistence.ReadModel;

public class OrderSummary
{
    public Guid      OrderId            { get; set; }
    public Guid      CustomerId         { get; set; }
    public string    CustomerEmail      { get; set; } = string.Empty;
    public string    Status             { get; set; } = string.Empty;
    public decimal   TotalAmount        { get; set; }
    public string    Currency           { get; set; } = string.Empty;
    public int       LinesCount         { get; set; }
    public string?   FirstItemName      { get; set; }
    public DateTime  CreatedAt          { get; set; }
    public DateTime? ConfirmedAt        { get; set; }
    public DateTime? CancelledAt        { get; set; }
    public string?   ShippingCity       { get; set; }
    public string?   CancellationReason { get; set; }
}
