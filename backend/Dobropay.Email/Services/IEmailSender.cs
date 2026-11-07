using Dobropay.Email.Models;

namespace Dobropay.Email.Services;

public interface IEmailSender
{
    Task SendAsync(EmailMessageRequest message, CancellationToken cancellationToken);
}
