namespace Dobropay.Payments.Services;

public sealed class ReconciliationOptions
{
    public required int IntervalSeconds { get; init; }
    public required int AttemptStaleSeconds { get; init; }
    public required int PaymentExpirationMinutes { get; init; }
    public required int CaptureExpirationMinutes { get; init; }
}
