using System.Text.Json.Serialization;

namespace Dobropay.Receipts.Models;

public sealed record DemoReceiptView(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("payment_id")] Guid PaymentId,
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("email_state")] string EmailState);
