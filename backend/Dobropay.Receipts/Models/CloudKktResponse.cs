namespace Dobropay.Receipts.Models;

public sealed record CloudKktResponse(
    long FiscalDocumentNumber,
    string FiscalStorageNumber,
    string FiscalAttribute,
    string? FiscalSign,
    string QrPayload,
    DateTimeOffset? RegisteredAt);
