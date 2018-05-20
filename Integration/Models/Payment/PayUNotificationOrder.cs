using System;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Payment
{
    public class PayuNotificationOrder
    {
        public string OrderId { get; set; }
        public string ExtOrderId { get; set; }
        public DateTime OrderCreatedDate { get; set; }
        public string NotifyUrl { get; set; }
        public string CustomerIp { get; set; }
        public string MerchantPosId { get; set; }
        public string Description { get; set; }
        public string CurrencyCode { get; set; }
        public int TotalAmount { get; set; }
        public string Status { get; set; }
    }
}