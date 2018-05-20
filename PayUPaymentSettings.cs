using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.PayuRedirect
{
    public class PayuPaymentSettings : ISettings
    {
        public string PosId
        {
            get;
            set;
        }

        public string OAuthClientId
        {
            get;
            set;
        }

        public string OAuthClientSecret
        {
            get;
            set;
        }

        public string SecondKey
        {
            get;
            set;
        }

        public string BaseUrl
        {
            get;
            set;
        }

        public int AdditionalFee
        {
            get;
            set;
        }

        public string Currency
        {
            get;
            set;
        }

        public TransactMode TransactMode
        {
            get;
            set;
        }
    }
}
