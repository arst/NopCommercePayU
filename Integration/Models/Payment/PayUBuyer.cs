using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Payment
{
    public class PayuBuyer
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }
    }
}