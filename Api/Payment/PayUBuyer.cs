namespace Nop.Plugin.Payments.PayU.Api.Payment
{
    public class PayUBuyer
    {
        public string email { get; set; }
        public string phone { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string language { get; set; }
    }
}