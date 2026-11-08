namespace Dobropay.Receipts.Models;

public sealed record FiscalReceiptItemDto(
    Guid Id,
    string Description,
    string Quantity,
    long Amount,
    int VatCode,
    string PaymentMode,
    string PaymentSubject)
{
    public string Name => Description;
    public long Price => Amount;
    public string Vat => $"vat{VatCode}";
}
