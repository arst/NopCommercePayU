namespace Nop.Plugin.Payments.PayuRedirect.Integration.Services
{
    internal interface IPayuAuthorizationService
    {
        string GetAuthToken();
    }
}