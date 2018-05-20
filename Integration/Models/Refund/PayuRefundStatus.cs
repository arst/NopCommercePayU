using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayuRedirect.Integration.Models.Refund
{
    public class PayuRefundStatus
    {
        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("statusDesc")]
        public string StatusDescription { get; set; }
    }
}