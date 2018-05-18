using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Refund
{
    public class PayuRefundResponse
    {
        [JsonProperty("orderId")]
        public string PayuOrderId { get; set; }

        [JsonProperty("status")]
        public PayuRefundStatus Status { get; set; }

        [JsonProperty("refund")]
        public PayuRefund Refund { get; set; }

        public bool Success => Status.StatusCode.Equals(PayuOrderStatusCode.Success, System.StringComparison.OrdinalIgnoreCase);
    }
}
