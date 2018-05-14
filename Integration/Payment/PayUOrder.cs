using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Payment
{
    class PayUOrder
    {
        [JsonProperty("notifyUrl")]
        public string NotifyUrl { get; set; }

        [JsonProperty("customerIp")]
        public string CustomerIp { get; set; }

        [JsonProperty("merchantPosId")]
        public string MerchantPosId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("currencyCode")]
        public string CurrencyCode { get; set; }

        [JsonProperty("totalAmount")]
        public int TotalAmount { get; set; }

        [JsonProperty("extOrderId")]
        public string ExtOrderId { get; set; }

        [JsonProperty("buyer")]
        public PayUBuyer Buyer { get; set; }

        [JsonProperty("products")]
        public List<PayUProduct> Products { get; set; }
    }
}
