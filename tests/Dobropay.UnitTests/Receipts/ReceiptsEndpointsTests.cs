using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Receipts.Api;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dobropay.UnitTests.Receipts;

public sealed class ReceiptsEndpointsTests
{
    private const string ExpectedInternalKey = "expected-internal-key";

    private readonly IInternalKeyValidator _keyValidator =
        new InternalKeyValidator(ExpectedInternalKey, new Sha256HashProvider());

    [Fact]
    public async Task HandleLookupAsync_QueryMatchingReceiptId_ReturnsOk()
    {
        var repository = new FakeReceiptsRepository();
        var receiptId = Guid.NewGuid();
        var receipt = CreateTestReceipt(receiptId, Guid.NewGuid(), 1042);
        await repository.SaveFiscalReceiptAsync(receipt, CancellationToken.None);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString($"?query={receiptId}");

        var result = await ReceiptsEndpoints.HandleLookupAsync(httpContext, repository, CancellationToken.None);

        var okResult = Assert.IsType<Ok<FiscalReceiptDto>>(result);
        Assert.Equal(receiptId, okResult.Value?.Id);
        Assert.Equal(1042L, okResult.Value?.FiscalDocumentNumber);
    }

    [Fact]
    public async Task HandleLookupAsync_QueryMatchingPaymentId_ReturnsOk()
    {
        var repository = new FakeReceiptsRepository();
        var receiptId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var receipt = CreateTestReceipt(receiptId, paymentId, 2048);
        await repository.SaveFiscalReceiptAsync(receipt, CancellationToken.None);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString($"?query={paymentId}");

        var result = await ReceiptsEndpoints.HandleLookupAsync(httpContext, repository, CancellationToken.None);

        var okResult = Assert.IsType<Ok<FiscalReceiptDto>>(result);
        Assert.Equal(paymentId, okResult.Value?.PaymentId);
        Assert.Equal(2048L, okResult.Value?.FiscalDocumentNumber);
    }

    [Fact]
    public async Task HandleLookupAsync_QueryMatchingDocNumber_ReturnsOk()
    {
        var repository = new FakeReceiptsRepository();
        var receiptId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var receipt = CreateTestReceipt(receiptId, paymentId, 4021);
        await repository.SaveFiscalReceiptAsync(receipt, CancellationToken.None);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?query=4021");

        var result = await ReceiptsEndpoints.HandleLookupAsync(httpContext, repository, CancellationToken.None);

        var okResult = Assert.IsType<Ok<FiscalReceiptDto>>(result);
        Assert.Equal(4021L, okResult.Value?.FiscalDocumentNumber);
        Assert.Equal(receiptId, okResult.Value?.Id);
    }

    [Fact]
    public async Task HandleLookupAsync_ExplicitParams_ReturnsOk()
    {
        var repository = new FakeReceiptsRepository();
        var receiptId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var receipt = CreateTestReceipt(receiptId, paymentId, 5001);
        await repository.SaveFiscalReceiptAsync(receipt, CancellationToken.None);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString($"?payment_id={paymentId}&fiscal_document_number=5001");

        var result = await ReceiptsEndpoints.HandleLookupAsync(httpContext, repository, CancellationToken.None);

        var okResult = Assert.IsType<Ok<FiscalReceiptDto>>(result);
        Assert.Equal(5001L, okResult.Value?.FiscalDocumentNumber);
    }

    [Fact]
    public async Task HandleLookupAsync_NoParameters_ReturnsBadRequest()
    {
        var repository = new FakeReceiptsRepository();
        var httpContext = new DefaultHttpContext();

        var result = await ReceiptsEndpoints.HandleLookupAsync(httpContext, repository, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ReceiptErrorResponse>>(result);
        Assert.Equal(ReceiptsEndpoints.QueryRequiredErrorCode, badRequest.Value?.Error);
    }

    [Fact]
    public async Task HandleLookupAsync_NotFound_ReturnsNotFound()
    {
        var repository = new FakeReceiptsRepository();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?query=99999999");

        var result = await ReceiptsEndpoints.HandleLookupAsync(httpContext, repository, CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ReceiptErrorResponse>>(result);
        Assert.Equal(ReceiptsEndpoints.ReceiptNotFoundErrorCode, notFound.Value?.Error);
    }

    [Fact]
    public async Task HandleGetByIdAsync_Exists_ReturnsOk()
    {
        var repository = new FakeReceiptsRepository();
        var receiptId = Guid.NewGuid();
        var receipt = CreateTestReceipt(receiptId, Guid.NewGuid(), 100);
        await repository.SaveFiscalReceiptAsync(receipt, CancellationToken.None);

        var result = await ReceiptsEndpoints.HandleGetByIdAsync(receiptId, repository, CancellationToken.None);

        var okResult = Assert.IsType<Ok<FiscalReceiptDto>>(result);
        Assert.Equal(receiptId, okResult.Value?.Id);
    }

    [Fact]
    public async Task HandleGetByIdAsync_NotFound_ReturnsNotFound()
    {
        var repository = new FakeReceiptsRepository();
        var result = await ReceiptsEndpoints.HandleGetByIdAsync(Guid.NewGuid(), repository, CancellationToken.None);

        var notFound = Assert.IsType<NotFound<ReceiptErrorResponse>>(result);
        Assert.Equal(ReceiptsEndpoints.ReceiptNotFoundErrorCode, notFound.Value?.Error);
    }

    [Fact]
    public async Task HandleShopReceiptsAsync_MissingOrInvalidInternalKey_ReturnsUnauthorized()
    {
        var repository = new FakeReceiptsRepository();
        var httpContext = new DefaultHttpContext();

        var resultMissing = await ReceiptsEndpoints.HandleShopReceiptsAsync(
            httpContext,
            Guid.NewGuid(),
            _keyValidator,
            repository,
            CancellationToken.None);
        Assert.IsType<UnauthorizedHttpResult>(resultMissing);

        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = "invalid-key";
        var resultInvalid = await ReceiptsEndpoints.HandleShopReceiptsAsync(
            httpContext,
            Guid.NewGuid(),
            _keyValidator,
            repository,
            CancellationToken.None);
        Assert.IsType<UnauthorizedHttpResult>(resultInvalid);
    }

    [Fact]
    public async Task HandleShopReceiptsAsync_ValidInternalKey_ReturnsShopReceipts()
    {
        var repository = new FakeReceiptsRepository();
        var shopId = Guid.NewGuid();
        var receipt1 = CreateTestReceipt(Guid.NewGuid(), Guid.NewGuid(), 101, shopId);
        var receipt2 = CreateTestReceipt(Guid.NewGuid(), Guid.NewGuid(), 102, shopId);
        var otherReceipt = CreateTestReceipt(Guid.NewGuid(), Guid.NewGuid(), 103, Guid.NewGuid());

        await repository.SaveFiscalReceiptAsync(receipt1, CancellationToken.None);
        await repository.SaveFiscalReceiptAsync(receipt2, CancellationToken.None);
        await repository.SaveFiscalReceiptAsync(otherReceipt, CancellationToken.None);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;

        var result = await ReceiptsEndpoints.HandleShopReceiptsAsync(
            httpContext,
            shopId,
            _keyValidator,
            repository,
            CancellationToken.None);

        var okResult = Assert.IsType<Ok<List<FiscalReceiptDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(2, okResult.Value.Count);
        Assert.All(okResult.Value, x => Assert.Equal(shopId, x.ShopId));
    }

    private static FiscalReceipt CreateTestReceipt(
        Guid id,
        Guid paymentId,
        long fd,
        Guid? shopId = null) =>
        new()
        {
            Id = id,
            ShopId = shopId ?? Guid.NewGuid(),
            PaymentId = paymentId,
            ReceiptType = ReceiptTypes.Payment,
            Status = ReceiptStatuses.Succeeded,
            TotalAmount = 150000,
            Currency = DomainConstants.DefaultCurrency,
            TaxSystemCode = DomainConstants.DefaultTaxSystemCode,
            CustomerEmail = "customer@dobropay.local",
            FiscalDocumentNumber = fd,
            FiscalStorageNumber = "9999078900012345",
            FiscalAttribute = "1234567890",
            FiscalSign = "1234567890",
            QrPayload = "t=20261108T1000&s=1500.00&fn=9999078900012345&i=1042&fp=1234567890&n=1",
            RegisteredAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            Items = new List<FiscalReceiptItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ReceiptId = id,
                    Description = "Керамическая кружка",
                    Quantity = "1",
                    Amount = 150000,
                    VatCode = DomainConstants.DefaultVatCode,
                    PaymentMode = FiscalPaymentModes.FullPayment,
                    PaymentSubject = FiscalPaymentSubjects.Commodity
                }
            }
        };
}
