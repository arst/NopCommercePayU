using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Payment
{

    class PayUOrderResponse
    {
        public string OrderId { get; set; }

        [JsonProperty("redirectUri")]
        public string RedirectUri { get; set; }
    }
}
