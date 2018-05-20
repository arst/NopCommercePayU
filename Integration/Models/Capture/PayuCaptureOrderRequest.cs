﻿using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Capture
{
    class PayuCaptureOrderRequest
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("orderStatus")]
        public string OrderStatus { get; set; }
    }
}
