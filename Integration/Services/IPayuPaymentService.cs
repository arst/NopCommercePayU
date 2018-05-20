using System;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.PayU.Integration.Models.Capture;
using Nop.Plugin.Payments.PayU.Integration.Models.Payment;
using Nop.Plugin.Payments.PayU.Integration.Models.Refund;

namespace Nop.Plugin.Payments.PayU.Integration.Services
{
    public interface IPayuPaymentService
    {
        PayuOrderResponse PlaceOrder(Order order, string customerIpAddress, string storeName, Uri storeUrl);

        PayuRefundResponse RequestRefund(Order order, decimal refundAmount, bool isPartial);

        PayuCaptureOrderResponse CapturePayment(Order order);
    }
}