using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.Payu
{
    public class RouteProvider : IRouteProvider
    {
        public int Priority
        {
            get
            {
                return 0;
            }
        }

        public void RegisterRoutes(RouteCollection routes)
        {
            RouteCollectionExtensions.MapRoute(routes, "Plugin.Payments.Payu.Configure", "Plugins/PaymentPayu/Configure", new
            {
                controller = "PaymentPayu",
                action = "Configure"
            }, new string[]
            {
                "Nop.Plugin.Payments.Payu.Controllers"
            });
            RouteCollectionExtensions.MapRoute(routes, "Plugin.Payments.Payu.PaymentInfo", "Plugins/PaymentPayu/PaymentInfo", new
            {
                controller = "PaymentPayu",
                action = "PaymentInfo"
            }, new string[]
            {
                "Nop.Plugin.Payments.Payu.Controllers"
            });
            RouteCollectionExtensions.MapRoute(routes, "Plugin.Payments.Payu.Return", "Plugins/PaymentPayu/Return", new
            {
                controller = "PaymentPayu",
                action = "Return"
            }, new string[]
            {
                "Nop.Plugin.Payments.Payu.Controllers"
            });
        }
    }
}
