using System;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models.Capture;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models.Payment;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models.Refund;

namespace Nop.Plugin.Payments.PayuRedirect.Integration.Services
{
    public interface IPayuPaymentService
    {
        PayuOrderResponse PlaceOrder(Order order, string customerIpAddress, string storeName, Uri storeUrl);

        PayuRefundResponse RequestRefund(Order order, decimal refundAmount, bool isPartial);

        PayuCaptureOrderResponse CapturePayment(Order order);
    }
}