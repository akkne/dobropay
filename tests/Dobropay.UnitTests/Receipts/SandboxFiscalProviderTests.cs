using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Services;

namespace Dobropay.UnitTests.Receipts;

public sealed class SandboxFiscalProviderTests
{
    [Fact]
    public async Task RegisterReceiptAsync_ValidPaymentCommand_GeneratesValid54FzRequisites()
    {
        var provider = new SandboxFiscalProvider();
        var receiptId = Guid.NewGuid();
        var command = new FiscalRegistrationCommand(
            receiptId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ReceiptTypes.Payment,
            129900,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            "buyer@example.com",
            "+79991234567",
            new List<ReceiptItemDto>
            {
                new("Керамическая кружка Dobropay", "1", 129900, DomainConstants.DefaultVatCode)
            });

        var result = await provider.RegisterReceiptAsync(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.FiscalDocumentNumber > 0);
        Assert.Equal(16, result.FiscalStorageNumber.Length);
        Assert.All(result.FiscalStorageNumber, c => Assert.True(char.IsDigit(c)));
        Assert.Equal(10, result.FiscalAttribute.Length);
        Assert.All(result.FiscalAttribute, c => Assert.True(char.IsDigit(c)));
        Assert.Equal(result.FiscalAttribute, result.FiscalSign);

        Assert.Contains($"s=1299.00", result.QrPayload);
        Assert.Contains($"fn={result.FiscalStorageNumber}", result.QrPayload);
        Assert.Contains($"i={result.FiscalDocumentNumber}", result.QrPayload);
        Assert.Contains($"fp={result.FiscalAttribute}", result.QrPayload);
        Assert.Contains("&n=1", result.QrPayload);
    }

    [Fact]
    public async Task RegisterReceiptAsync_RefundCommand_SetsOperationTypeTwo()
    {
        var provider = new SandboxFiscalProvider();
        var command = new FiscalRegistrationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReceiptTypes.Refund,
            5000,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            "refund@example.com",
            null,
            new List<ReceiptItemDto>
            {
                new("Возврат товара", "1", 5000, DomainConstants.DefaultVatCode)
            });

        var result = await provider.RegisterReceiptAsync(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("s=50.00", result.QrPayload);
        Assert.Contains("&n=2", result.QrPayload);
    }

    [Fact]
    public async Task RegisterReceiptAsync_SameReceiptId_ProducesDeterministicRequisites()
    {
        var provider = new SandboxFiscalProvider();
        var receiptId = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        var command1 = new FiscalRegistrationCommand(
            receiptId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ReceiptTypes.Payment,
            75050,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            null,
            null,
            Array.Empty<ReceiptItemDto>());

        var command2 = new FiscalRegistrationCommand(
            receiptId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ReceiptTypes.Payment,
            75050,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            null,
            null,
            Array.Empty<ReceiptItemDto>());

        var result1 = await provider.RegisterReceiptAsync(command1, CancellationToken.None);
        var result2 = await provider.RegisterReceiptAsync(command2, CancellationToken.None);

        Assert.Equal(result1.FiscalDocumentNumber, result2.FiscalDocumentNumber);
        Assert.Equal(result1.FiscalStorageNumber, result2.FiscalStorageNumber);
        Assert.Equal(result1.FiscalAttribute, result2.FiscalAttribute);
    }

    [Theory]
    [InlineData(12345, "123.45")]
    [InlineData(100, "1.00")]
    [InlineData(99, "0.99")]
    [InlineData(5, "0.05")]
    [InlineData(0, "0.00")]
    public async Task RegisterReceiptAsync_FormatsKopecksCorrectly(long amount, string expectedSum)
    {
        var provider = new SandboxFiscalProvider();
        var command = new FiscalRegistrationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ReceiptTypes.Payment,
            amount,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            null,
            null,
            Array.Empty<ReceiptItemDto>());

        var result = await provider.RegisterReceiptAsync(command, CancellationToken.None);

        Assert.Contains($"s={expectedSum}", result.QrPayload);
    }
}
