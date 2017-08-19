using System.Collections.Generic;

namespace Nop.Plugin.Payments.PayU.Api.Payment
{
    class PayUOrder
    {
        public string notifyUrl { get; set; }
        public string customerIp { get; set; }
        public string merchantPosId { get; set; }
        public string description { get; set; }
        public string currencyCode { get; set; }
        public int totalAmount { get; set; }
        public string extOrderId { get; set; }
        public PayUBuyer buyer { get; set; }

        public List<PayUProduct> products { get; set; }
    }
}
