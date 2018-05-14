namespace Nop.Plugin.Payments.PayU.Api.Refund
{
    public class RefundStatus
    {
        public string StatusCode { get; set; }
        public string Severity { get; set; }
        public string StatusDescription { get; set; }
    }
}