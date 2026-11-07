namespace Dobropay.Messaging.Database;

public interface IDatabaseHealthChecker
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
