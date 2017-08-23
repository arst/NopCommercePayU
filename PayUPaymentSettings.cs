using Nop.Core.Configuration;
using System;

namespace Nop.Plugin.Payments.Payu
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
    }
}
