namespace Dobropay.Receipts.Models;

public sealed class ReceiptsOptions
{
    public required string EmailServiceBaseUrl { get; init; }
    public required string InternalServiceKey { get; init; }
    public required string DemoReceiptEmail { get; init; }
}
