namespace Dobropay.Payments.Provider;

public interface ICheckoutTokenGenerator
{
    string GenerateToken(Guid paymentId);
}
