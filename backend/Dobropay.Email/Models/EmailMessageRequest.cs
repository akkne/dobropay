namespace Dobropay.Email.Models;

public sealed record EmailMessageRequest(Guid ReceiptId, string To, string Subject, string Text);
