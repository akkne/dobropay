using System.Net.Mail;
using Dobropay.Email.Models;

namespace Dobropay.Email.Services;

public sealed class SmtpEmailSender(SmtpOptions options) : IEmailSender
{
    public async Task SendAsync(EmailMessageRequest message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(options.Host, options.Port);
        using var mailMessage = new MailMessage(options.From, message.To, message.Subject, message.Text);
        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}
