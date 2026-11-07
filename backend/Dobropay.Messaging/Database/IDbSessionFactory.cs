namespace Dobropay.Messaging.Database;

public interface IDbSessionFactory
{
    Task<IDbSession> OpenSessionAsync(CancellationToken cancellationToken);
    Task<IDbSession> OpenShopSessionAsync(Guid shopId, CancellationToken cancellationToken);
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
