using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Payment
{

    public class PayuOrderResponse
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("redirectUri")]
        public string RedirectUri { get; set; }

        [JsonProperty("status")]
        public PayuOrderStatus Status { get; set; }
    }
}
