using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Payment
{

    public class PayUOrderResponse
    {
        public string OrderId { get; set; }

        [JsonProperty("redirectUri")]
        public string RedirectUri { get; set; }

        public PayUOrderStatus Status { get; set; }
    }
}
