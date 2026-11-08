using Dobropay.Receipts.Models;

namespace Dobropay.Receipts.Services;

public interface IFiscalProvider
{
    Task<FiscalRegistrationResult> RegisterReceiptAsync(FiscalRegistrationCommand command, CancellationToken cancellationToken);
}
