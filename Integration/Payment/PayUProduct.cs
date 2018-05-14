using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Payment
{
    public class PayUProduct
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("unitPrice")]
        public int UnitPrice { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}