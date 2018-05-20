using RestSharp;

namespace Nop.Plugin.Payments.PayuRedirect.Integration
{
    public interface IPayuRestClientFactory
    {
        RestClient GetApiClient(string relativePath);
    }
}