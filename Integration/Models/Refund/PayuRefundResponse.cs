using System.Collections.Generic;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Refund
{
    public class PayuRefundResponse
    {
        public PayuRefundResponse()
        {
            Errors = new List<string>();
        }

        public PayuRefundStatus Status { get; set; }

        public bool Success { get; set; }

        public List<string> Errors { get; set; }
    }
}
