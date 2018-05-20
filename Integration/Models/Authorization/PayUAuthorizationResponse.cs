using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayuRedirect.Integration.Models.Authorization
{
    class PayuAuthorizationResponse
    {
        [JsonProperty("Access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("Token_type")]
        public string TokenType { get; set; }

        [JsonProperty("Expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty("Grant_type")]
        public string GrantType { get; set; }
    }
}
