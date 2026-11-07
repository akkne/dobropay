namespace Dobropay.Contracts.Security;

public sealed class InternalKeyValidator(string expectedKey, IHashProvider hashProvider) : IInternalKeyValidator
{
    public bool IsValid(string? suppliedKey) =>
        !string.IsNullOrEmpty(suppliedKey) && hashProvider.FixedTimeEqualsSha256(expectedKey, suppliedKey);
}
