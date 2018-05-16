namespace Nop.Plugin.Payments.PayU.Integration.Models.Refund
{
    public class PayuRefundStatus
    {
        public string StatusCode { get; set; }

        public string Severity { get; set; }

        public string StatusDescription { get; set; }
    }
}