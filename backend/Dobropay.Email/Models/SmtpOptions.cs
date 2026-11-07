namespace Dobropay.Email.Models;

public sealed class SmtpOptions
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string From { get; init; }
}
