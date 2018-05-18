using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Capture
{
    public class PayuCaptureResultStatus
    {
        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("statusDesc")]
        public string StatusDesc { get; set; }
    }
}
