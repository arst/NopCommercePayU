using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayuRedirect.Integration.Models.Payment
{
    public class PayuOrderStatus
    {
        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        public bool IsSuccess => StatusCode.Equals(PayuOrderStatusCode.Success, System.StringComparison.OrdinalIgnoreCase);

        public bool IsWaitingForConfirmation => StatusCode.Equals(PayuOrderStatusCode.WaitingForConfirmation, System.StringComparison.OrdinalIgnoreCase);
    }
}