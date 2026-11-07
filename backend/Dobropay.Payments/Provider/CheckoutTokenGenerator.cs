using System.Text;
using Dobropay.Contracts.Security;

namespace Dobropay.Payments.Provider;

public sealed class CheckoutTokenGenerator(string checkoutSecret, IHashProvider hashProvider) : ICheckoutTokenGenerator
{
    public string GenerateToken(Guid paymentId) =>
        hashProvider.ComputeHmacSha256Hex(checkoutSecret, Encoding.UTF8.GetBytes(paymentId.ToString("N")));
}
