namespace Dobropay.Receipts.Persistence;

public static class ReceiptsSchemaConstants
{
    public const string ReceiptInboxTable = "receipt_inbox";
    public const string DemoReceiptsTable = "demo_receipts";
    public const string EmailStatePending = "pending";
    public const string EmailStateSending = "sending";
    public const string EmailStateSent = "sent";
    public const string EmailStateFailed = "failed";
}
