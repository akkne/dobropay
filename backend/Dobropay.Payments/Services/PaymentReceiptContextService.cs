using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;

namespace Dobropay.Payments.Services;

public sealed class PaymentReceiptContextService(
    IPaymentsRepository paymentsRepository,
    IRefundRepository refundRepository) : IPaymentReceiptContextService
{
    public async Task<PaymentReceiptContextResult> GetReceiptContextAsync(
        Guid shopId,
        Guid paymentId,
        Guid? refundId,
        CancellationToken cancellationToken)
    {
        var payment = await paymentsRepository.FindPaymentByIdAsync(shopId, paymentId, cancellationToken);
        if (payment is null)
        {
            return new PaymentReceiptContextResult(
                PaymentReceiptContextOutcome.PaymentNotFound,
                ErrorCode: ErrorCodes.PaymentNotFound);
        }

        Refund? refund = null;
        if (refundId.HasValue && refundId.Value != Guid.Empty)
        {
            refund = await refundRepository.FindByIdAsync(shopId, refundId.Value, cancellationToken);
            if (refund is null || refund.PaymentId != paymentId)
            {
                return new PaymentReceiptContextResult(
                    PaymentReceiptContextOutcome.RefundNotFound,
                    ErrorCode: ErrorCodes.RefundNotFound);
            }
        }

        var payload = await paymentsRepository.FindReceiptPayloadAsync(
            shopId,
            paymentId,
            refund?.Id,
            cancellationToken);

        var isRefund = refund is not null;
        var amount = isRefund ? refund!.Amount : payment.Amount;
        var currency = isRefund ? refund!.Currency : payment.Currency;
        var receiptType = isRefund ? ReceiptTypes.Refund : ReceiptTypes.Payment;
        var defaultDescription = isRefund
            ? $"Возврат по заказу {payment.ExternalOrderId ?? payment.Id.ToString()}"
            : $"Оплата заказа {payment.ExternalOrderId ?? payment.Id.ToString()}";

        if (payload is null)
        {
            var defaultItems = new[]
            {
                new ReceiptItemDto(defaultDescription, "1", amount)
            };

            var defaultContext = new PaymentReceiptContextDto(
                shopId,
                payment.Id,
                refund?.Id,
                receiptType,
                amount,
                currency,
                payment.ExternalOrderId,
                payment.PaymentMethodType,
                DomainConstants.DefaultTaxSystemCode,
                CustomerEmail: null,
                CustomerPhone: null,
                defaultItems);

            return new PaymentReceiptContextResult(PaymentReceiptContextOutcome.Success, defaultContext);
        }

        IReadOnlyList<ReceiptItemDto>? items = null;
        if (!string.IsNullOrWhiteSpace(payload.ItemsJson))
        {
            try
            {
                items = JsonSerializer.Deserialize<IReadOnlyList<ReceiptItemDto>>(payload.ItemsJson);
            }
            catch
            {
                items = null;
            }
        }

        if (items is null || items.Count == 0)
        {
            items = [new ReceiptItemDto(defaultDescription, "1", amount)];
        }

        var context = new PaymentReceiptContextDto(
            shopId,
            payment.Id,
            refund?.Id,
            receiptType,
            amount,
            currency,
            payment.ExternalOrderId,
            payment.PaymentMethodType,
            payload.TaxSystemCode,
            payload.CustomerEmail,
            payload.CustomerPhone,
            items);

        return new PaymentReceiptContextResult(PaymentReceiptContextOutcome.Success, context);
    }
}
