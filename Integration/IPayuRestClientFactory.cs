using RestSharp;

namespace Nop.Plugin.Payments.PayU.Integration
{
    public interface IPayuRestClientFactory
    {
        RestClient GetApiClient(string relativePath);
    }
}