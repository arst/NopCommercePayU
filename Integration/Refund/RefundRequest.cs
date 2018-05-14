namespace Nop.Plugin.Payments.PayU.Api.Refund
{
    public class RefundRequest
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
