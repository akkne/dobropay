using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var serviceKey = builder.Configuration["INTERNAL_SERVICE_KEY"] ?? throw new InvalidOperationException("INTERNAL_SERVICE_KEY missing");
var host = builder.Configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host missing");
var port = builder.Configuration.GetValue<int>("Smtp:Port");
var from = builder.Configuration["Smtp:From"] ?? throw new InvalidOperationException("Smtp:From missing");
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
app.MapPost("/internal/v1/email", async (HttpContext http, EmailMessage body, CancellationToken ct) =>
{
    var supplied = http.Request.Headers["X-Internal-Key"].ToString();
    if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(serviceKey)), SHA256.HashData(Encoding.UTF8.GetBytes(supplied))))
        return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(body.To) || body.Subject.Length > 200 || body.Text.Length > 10000) return Results.BadRequest();
    using var client = new SmtpClient(host, port);
    using var message = new MailMessage(from, body.To, body.Subject, body.Text);
    await client.SendMailAsync(message, ct);
    app.Logger.LogInformation("Email accepted for receipt {ReceiptId} correlation {CorrelationId}", body.ReceiptId, http.TraceIdentifier);
    return Results.Accepted();
});

app.Run();

public sealed record EmailMessage(Guid ReceiptId, string To, string Subject, string Text);
