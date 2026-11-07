namespace Dobropay.Contracts.Security;

public interface IInternalKeyValidator
{
    bool IsValid(string? suppliedKey);
}
