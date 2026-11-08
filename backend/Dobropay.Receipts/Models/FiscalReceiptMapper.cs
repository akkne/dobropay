using Dobropay.Receipts.Persistence;

namespace Dobropay.Receipts.Models;

public static class FiscalReceiptMapper
{
    public static FiscalReceiptDto ToDto(FiscalReceipt receipt) =>
        new(
            receipt.Id,
            receipt.ShopId,
            receipt.PaymentId,
            receipt.RefundId,
            receipt.ReceiptType,
            receipt.Status,
            receipt.TotalAmount,
            receipt.Currency,
            receipt.TaxSystemCode,
            receipt.CustomerEmail,
            receipt.CustomerPhone,
            receipt.FiscalDocumentNumber,
            receipt.FiscalStorageNumber,
            receipt.FiscalAttribute,
            receipt.FiscalSign,
            receipt.QrPayload,
            receipt.RegisteredAt,
            receipt.CreatedAt,
            receipt.Items.Select(x => new FiscalReceiptItemDto(
                x.Id,
                x.Description,
                x.Quantity,
                x.Amount,
                x.VatCode,
                x.PaymentMode,
                x.PaymentSubject)).ToList());
}
