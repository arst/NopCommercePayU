using System;
using RestSharp;

namespace Nop.Plugin.Payments.PayU.Integration
{
    public class PayuRestClientFactory : IPayuRestClientFactory
    {
        private readonly PayuPaymentSettings paymentSettings;

        public PayuRestClientFactory(PayuPaymentSettings payuPaymentSettings)
        {
            paymentSettings = payuPaymentSettings;
        }

        public virtual RestClient GetApiClient(string relativePath)
        {
            var baseUri = new Uri(paymentSettings.BaseUrl);
            var relativeUri = new Uri(baseUri, relativePath);
            RestClient client = new RestClient(relativeUri);
            client.FollowRedirects = false;

            return client;
        }
    }
}
