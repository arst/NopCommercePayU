using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Capture
{
    public class PayuCaptureOrderResponse
    {
        [JsonProperty("status")]
        public PayuCaptureResultStatus Status { get; set; }

        public bool IsSuccess => Status.StatusCode.Equals(PayuOrderStatusCode.Success, StringComparison.OrdinalIgnoreCase);
    }
}
