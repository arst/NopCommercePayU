using Newtonsoft.Json;
using System;

namespace Nop.Plugin.Payments.PayuRedirect.Integration.Models.Refund
{
    public class PayuRefund
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("refundId")]
        public string RefundId { get; set; }

        [JsonProperty("extRefundId")]
        public int ExtRefundId { get; set; }

        [JsonProperty("currencyCode")]
        public string CurrencyCode { get; set; }

        [JsonProperty("creationDateTime")]
        public DateTime CreationDateTime { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusDateTime")]
        public DateTime StatusDateTime { get; set; }
    }
}
