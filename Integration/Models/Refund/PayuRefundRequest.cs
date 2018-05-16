namespace Nop.Plugin.Payments.PayU.Integration.Models.Refund
{
    public class PayuRefundRequest
    {
        public string Description
        {
            get
            {
                return "Refund";
            }
        }

        public string Amount { get; set; }
    }
}
