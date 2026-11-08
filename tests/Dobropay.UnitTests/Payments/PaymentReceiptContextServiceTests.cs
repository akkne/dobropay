using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Services;

namespace Dobropay.UnitTests.Payments;

public sealed class PaymentReceiptContextServiceTests
{
    [Fact]
    public async Task GetReceiptContextAsync_PaymentWithCustomPayload_ReturnsContextWithCustomItems()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var service = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 150000,
            Currency = DomainConstants.DefaultCurrency,
            ExternalOrderId = "ORDER-777",
            PaymentMethodType = PaymentMethodTypes.Sandbox,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment);

        var customItems = new List<ReceiptItemDto>
        {
            new("Курс программирования", "1.000", 100000, 1, FiscalPaymentModes.FullPayment, FiscalPaymentSubjects.Service),
            new("Методические материалы", "1.000", 50000, 1, FiscalPaymentModes.FullPayment, FiscalPaymentSubjects.Commodity)
        };

        var payload = new PaymentReceiptPayload
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            PaymentId = payment.Id,
            RefundId = null,
            ReceiptType = ReceiptTypes.Payment,
            TaxSystemCode = 2,
            CustomerEmail = "customer@dobropay.test",
            CustomerPhone = "+79991112233",
            ItemsJson = JsonSerializer.Serialize(customItems),
            CreatedAt = DateTimeOffset.UtcNow
        };
        paymentsRepository.ReceiptPayloads.Add(payload);

        var result = await service.GetReceiptContextAsync(shopId, payment.Id, null, CancellationToken.None);

        Assert.Equal(PaymentReceiptContextOutcome.Success, result.Outcome);
        Assert.NotNull(result.Context);
        var context = result.Context;
        Assert.Equal(shopId, context.ShopId);
        Assert.Equal(payment.Id, context.PaymentId);
        Assert.Null(context.RefundId);
        Assert.Equal(ReceiptTypes.Payment, context.ReceiptType);
        Assert.Equal(payment.Amount, context.Amount);
        Assert.Equal(DomainConstants.DefaultCurrency, context.Currency);
        Assert.Equal("ORDER-777", context.ExternalOrderId);
        Assert.Equal(PaymentMethodTypes.Sandbox, context.PaymentMethodType);
        Assert.Equal(2, context.TaxSystemCode);
        Assert.Equal("customer@dobropay.test", context.CustomerEmail);
        Assert.Equal("+79991112233", context.CustomerPhone);
        Assert.Equal(2, context.Items.Count);
        Assert.Equal("Курс программирования", context.Items[0].Description);
        Assert.Equal("1.000", context.Items[0].Quantity);
        Assert.Equal(100000, context.Items[0].Amount);
        Assert.Equal("Методические материалы", context.Items[1].Description);
    }

    [Fact]
    public async Task GetReceiptContextAsync_PaymentWithoutPayload_ReturnsDefaultContext()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var service = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 45000,
            Currency = DomainConstants.DefaultCurrency,
            ExternalOrderId = "ORDER-888",
            PaymentMethodType = PaymentMethodTypes.Sbp,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment);

        var result = await service.GetReceiptContextAsync(shopId, payment.Id, null, CancellationToken.None);

        Assert.Equal(PaymentReceiptContextOutcome.Success, result.Outcome);
        Assert.NotNull(result.Context);
        var context = result.Context;
        Assert.Equal(shopId, context.ShopId);
        Assert.Equal(payment.Id, context.PaymentId);
        Assert.Null(context.RefundId);
        Assert.Equal(ReceiptTypes.Payment, context.ReceiptType);
        Assert.Equal(45000, context.Amount);
        Assert.Equal(DomainConstants.DefaultCurrency, context.Currency);
        Assert.Equal("ORDER-888", context.ExternalOrderId);
        Assert.Equal(PaymentMethodTypes.Sbp, context.PaymentMethodType);
        Assert.Equal(DomainConstants.DefaultTaxSystemCode, context.TaxSystemCode);
        Assert.Null(context.CustomerEmail);
        Assert.Null(context.CustomerPhone);
        Assert.Single(context.Items);

        var item = context.Items[0];
        Assert.Equal("Оплата заказа ORDER-888", item.Description);
        Assert.Equal("1", item.Quantity);
        Assert.Equal(45000, item.Amount);
        Assert.Equal(DomainConstants.DefaultVatCode, item.VatCode);
        Assert.Equal(FiscalPaymentModes.FullPayment, item.PaymentMode);
        Assert.Equal(FiscalPaymentSubjects.Commodity, item.PaymentSubject);
    }

    [Fact]
    public async Task GetReceiptContextAsync_PaymentWithoutExternalOrderId_UsesPaymentIdInDefaultDescription()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var service = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 30000,
            Currency = DomainConstants.DefaultCurrency,
            ExternalOrderId = null,
            PaymentMethodType = PaymentMethodTypes.BankCard,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment);

        var result = await service.GetReceiptContextAsync(shopId, payment.Id, null, CancellationToken.None);

        Assert.Equal(PaymentReceiptContextOutcome.Success, result.Outcome);
        Assert.NotNull(result.Context);
        Assert.Single(result.Context.Items);
        Assert.Equal($"Оплата заказа {payment.Id}", result.Context.Items[0].Description);
        Assert.Equal("1", result.Context.Items[0].Quantity);
        Assert.Equal(30000, result.Context.Items[0].Amount);
    }

    [Fact]
    public async Task GetReceiptContextAsync_RefundWithCustomPayload_ReturnsRefundContext()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var service = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 100000,
            Currency = DomainConstants.DefaultCurrency,
            ExternalOrderId = "ORDER-REF-1",
            PaymentMethodType = PaymentMethodTypes.Sandbox,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment);

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            PaymentId = payment.Id,
            Amount = 35000,
            Currency = DomainConstants.DefaultCurrency,
            Status = RefundStatuses.Succeeded,
            CreatedAt = DateTimeOffset.UtcNow
        };
        refundRepository.Refunds.Add(refund);

        var customRefundItems = new List<ReceiptItemDto>
        {
            new("Частичный возврат за товар", "1.000", 35000, 1)
        };

        var payload = new PaymentReceiptPayload
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            PaymentId = payment.Id,
            RefundId = refund.Id,
            ReceiptType = ReceiptTypes.Refund,
            TaxSystemCode = 1,
            CustomerEmail = "refund-user@dobropay.test",
            CustomerPhone = "+79995554433",
            ItemsJson = JsonSerializer.Serialize(customRefundItems),
            CreatedAt = DateTimeOffset.UtcNow
        };
        paymentsRepository.ReceiptPayloads.Add(payload);

        var result = await service.GetReceiptContextAsync(shopId, payment.Id, refund.Id, CancellationToken.None);

        Assert.Equal(PaymentReceiptContextOutcome.Success, result.Outcome);
        Assert.NotNull(result.Context);
        var context = result.Context;
        Assert.Equal(shopId, context.ShopId);
        Assert.Equal(payment.Id, context.PaymentId);
        Assert.Equal(refund.Id, context.RefundId);
        Assert.Equal(ReceiptTypes.Refund, context.ReceiptType);
        Assert.Equal(35000, context.Amount);
        Assert.Equal(1, context.TaxSystemCode);
        Assert.Equal("refund-user@dobropay.test", context.CustomerEmail);
        Assert.Equal("+79995554433", context.CustomerPhone);
        Assert.Single(context.Items);
        Assert.Equal("Частичный возврат за товар", context.Items[0].Description);
        Assert.Equal(35000, context.Items[0].Amount);
    }

    [Fact]
    public async Task GetReceiptContextAsync_RefundWithoutPayload_ReturnsDefaultRefundContext()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var service = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 80000,
            Currency = DomainConstants.DefaultCurrency,
            ExternalOrderId = "ORDER-REF-DEFAULT",
            PaymentMethodType = PaymentMethodTypes.Sandbox,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment);

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            PaymentId = payment.Id,
            Amount = 40000,
            Currency = DomainConstants.DefaultCurrency,
            Status = RefundStatuses.Succeeded,
            CreatedAt = DateTimeOffset.UtcNow
        };
        refundRepository.Refunds.Add(refund);

        var result = await service.GetReceiptContextAsync(shopId, payment.Id, refund.Id, CancellationToken.None);

        Assert.Equal(PaymentReceiptContextOutcome.Success, result.Outcome);
        Assert.NotNull(result.Context);
        var context = result.Context;
        Assert.Equal(shopId, context.ShopId);
        Assert.Equal(payment.Id, context.PaymentId);
        Assert.Equal(refund.Id, context.RefundId);
        Assert.Equal(ReceiptTypes.Refund, context.ReceiptType);
        Assert.Equal(40000, context.Amount);
        Assert.Equal(DomainConstants.DefaultTaxSystemCode, context.TaxSystemCode);
        Assert.Null(context.CustomerEmail);
        Assert.Null(context.CustomerPhone);
        Assert.Single(context.Items);
        Assert.Equal("Возврат по заказу ORDER-REF-DEFAULT", context.Items[0].Description);
        Assert.Equal("1", context.Items[0].Quantity);
        Assert.Equal(40000, context.Items[0].Amount);
    }

    [Fact]
    public async Task GetReceiptContextAsync_PaymentNotFound_ReturnsPaymentNotFoundOutcome()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var service = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var result = await service.GetReceiptContextAsync(Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);

        Assert.Equal(PaymentReceiptContextOutcome.PaymentNotFound, result.Outcome);
        Assert.Equal(ErrorCodes.PaymentNotFound, result.ErrorCode);
        Assert.Null(result.Context);
    }

    [Fact]
    public async Task GetReceiptContextAsync_RefundNotFound_ReturnsRefundNotFoundOutcome()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var service = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment);

        var result = await service.GetReceiptContextAsync(shopId, payment.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(PaymentReceiptContextOutcome.RefundNotFound, result.Outcome);
        Assert.Equal(ErrorCodes.RefundNotFound, result.ErrorCode);
        Assert.Null(result.Context);
    }

    [Fact]
    public async Task GetReceiptContextAsync_RefundBelongsToDifferentPayment_ReturnsRefundNotFoundOutcome()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var service = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            Status = PaymentRules.Succeeded
        };
        var payment2 = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment1);
        paymentsRepository.Payments.Add(payment2);

        var refundForPayment2 = new Refund
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            PaymentId = payment2.Id,
            Amount = 25000,
            Status = RefundStatuses.Succeeded
        };
        refundRepository.Refunds.Add(refundForPayment2);

        var result = await service.GetReceiptContextAsync(shopId, payment1.Id, refundForPayment2.Id, CancellationToken.None);

        Assert.Equal(PaymentReceiptContextOutcome.RefundNotFound, result.Outcome);
        Assert.Equal(ErrorCodes.RefundNotFound, result.ErrorCode);
        Assert.Null(result.Context);
    }
}
