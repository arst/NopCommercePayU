namespace Nop.Plugin.Payments.PayU.Integration.Models.Payment
{
    public class PayUOrderStatus
    {
        public string StatusCode { get; set; }

        public bool IsSuccess => StatusCode.Equals(PayuOrderStatusCode.Success, System.StringComparison.OrdinalIgnoreCase);

        public bool IsWaitingForConfirmation => StatusCode.Equals(PayuOrderStatusCode.WaitingForConfirmation, System.StringComparison.OrdinalIgnoreCase);
    }
}