namespace Dobropay.Receipts.Models;

public sealed record FiscalRegistrationResult(
    bool Success,
    long FiscalDocumentNumber,
    string FiscalStorageNumber,
    string FiscalAttribute,
    string? FiscalSign,
    string QrPayload,
    DateTimeOffset RegisteredAt,
    string? ErrorMessage = null)
{
    public static FiscalRegistrationResult Succeeded(
        long fiscalDocumentNumber,
        string fiscalStorageNumber,
        string fiscalAttribute,
        string? fiscalSign,
        string qrPayload,
        DateTimeOffset registeredAt) =>
        new(true, fiscalDocumentNumber, fiscalStorageNumber, fiscalAttribute, fiscalSign, qrPayload, registeredAt);

    public static FiscalRegistrationResult Failed(string errorMessage) =>
        new(false, 0, string.Empty, string.Empty, null, string.Empty, DateTimeOffset.UtcNow, errorMessage);
}
