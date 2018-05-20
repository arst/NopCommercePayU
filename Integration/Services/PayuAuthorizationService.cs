using System.Security;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models.Authorization;
using RestSharp;

namespace Nop.Plugin.Payments.PayuRedirect.Integration.Services
{
    public class PayuAuthorizationService : IPayuAuthorizationService
    {
        private readonly IPayuRestClientFactory clientFactory;
        private readonly PayuPaymentSettings payuPaymentSettings;

        public PayuAuthorizationService(IPayuRestClientFactory clientFactory, PayuPaymentSettings payuPaymentSettings)
        {
            this.clientFactory = clientFactory;
            this.payuPaymentSettings = payuPaymentSettings;
        }

        public virtual string GetAuthToken()
        {
            var securityClient = clientFactory.GetApiClient("/pl/standard/user/oauth");
            var securityRequest = new RestRequest("authorize", Method.POST);
            securityRequest.AddParameter("grant_type", "client_credentials");
            securityRequest.AddParameter("client_id", payuPaymentSettings.PosId);
            securityRequest.AddParameter("client_secret", payuPaymentSettings.OAuthClientSecret);
            var response = securityClient.Execute<PayuAuthorizationResponse>(securityRequest);
            var accToken = response.Data.AccessToken;

            if (string.IsNullOrEmpty(accToken))
            {
                throw new SecurityException("PayU can't generate bearer token. Check payment method setting or contact responsible person.");
            }
            return accToken;
        }
    }
}
