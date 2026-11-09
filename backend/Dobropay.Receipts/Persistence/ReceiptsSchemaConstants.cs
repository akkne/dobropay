namespace Dobropay.Receipts.Persistence;

public static class ReceiptsSchemaConstants
{
    public const string ReceiptInboxTable = "receipt_inbox";
    public const string FiscalReceiptsTable = "fiscal_receipts";
    public const string FiscalReceiptItemsTable = "fiscal_receipt_items";


    public const string NowDefaultValueExpression = "NOW()";

    public const int ReceiptTypeMaxLength = 32;
    public const int StatusMaxLength = 32;
    public const int CurrencyMaxLength = 8;
    public const int CustomerEmailMaxLength = 256;
    public const int CustomerPhoneMaxLength = 32;
    public const int FiscalStorageNumberMaxLength = 32;
    public const int FiscalAttributeMaxLength = 32;
    public const int FiscalSignMaxLength = 64;
    public const int QrPayloadMaxLength = 512;

    public const int ItemDescriptionMaxLength = 256;
    public const int ItemQuantityMaxLength = 32;
    public const int PaymentModeMaxLength = 32;
    public const int PaymentSubjectMaxLength = 32;
}
