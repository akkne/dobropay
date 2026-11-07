using Dobropay.Contracts.Constants;

namespace Dobropay.Contracts.Receipts;

public sealed record ReceiptItemDto(
    string Description,
    string Quantity,
    long Amount,
    int VatCode = DomainConstants.DefaultVatCode,
    string PaymentMode = FiscalPaymentModes.FullPayment,
    string PaymentSubject = FiscalPaymentSubjects.Commodity);
