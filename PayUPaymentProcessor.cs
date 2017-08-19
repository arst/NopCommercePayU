using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.Payu.Controllers;
using Nop.Plugin.Payments.PayU.Api.Authorization;
using Nop.Plugin.Payments.PayU.Api.Payment;
using Nop.Plugin.Payments.PayU.Infrastructure;
using Nop.Plugin.Payments.PayU.Models;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Routing;

namespace Nop.Plugin.Payments.Payu
{
    public class PayuPaymentProcessor : BasePlugin, IPaymentMethod, IPlugin
    {
        private readonly PayuPaymentSettings _PayuPaymentSettings;

        private readonly ISettingService _settingService;

        private readonly ICurrencyService _currencyService;

        private readonly CurrencySettings _currencySettings;

        private readonly IWebHelper _webHelper;

        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return 0;
            }
        }

        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public string PaymentMethodDescription => "PayU";

        public PayuPaymentProcessor(PayuPaymentSettings PayuPaymentSettings, ISettingService settingService, ICurrencyService currencyService, CurrencySettings currencySettings, IWebHelper webHelper)
        {
            this._PayuPaymentSettings = PayuPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            ProcessPaymentResult result = new ProcessPaymentResult();
            result.NewPaymentStatus = Core.Domain.Payments.PaymentStatus.Pending;
            if (!(this._currencyService.GetCurrencyByCode("PZL") != null) || !this._currencyService.GetCurrencyByCode("PZL").Published)
            {
                result.AddError("You need to enable PZL currency from nopcommerce admin. Go to Settings=> Currency and add PZL Currencies Or Contact the admin.");
            }
            return result;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {            
            RestClient cl = new RestClient(this._PayuPaymentSettings.BaseUrl.TrimEnd('/') + "/api/v2_1/");
            cl.FollowRedirects = false;
            var request = new RestRequest("orders", Method.POST);
            request.AddHeader("Content-Type", "application/json");
            
            PayUOrder order = new PayUOrder();
            order.currencyCode = this._currencyService.GetCurrencyById(this._currencySettings.PrimaryStoreCurrencyId).CurrencyCode;
            order.customerIp = _webHelper.GetCurrentIpAddress();
            order.description = "Test order";
            order.extOrderId = postProcessPaymentRequest.Order.Id.ToString();
            order.merchantPosId = this._PayuPaymentSettings.PosId;
            order.notifyUrl = this._webHelper.GetStoreLocation(false) + "Plugins/PaymentPayu/Return";
            order.totalAmount = (int)(postProcessPaymentRequest.Order.OrderTotal * 100);
            order.buyer = new PayUBuyer()
            {
                email = postProcessPaymentRequest.Order.BillingAddress.Email,
                firstName = postProcessPaymentRequest.Order.BillingAddress.FirstName,
                lastName = postProcessPaymentRequest.Order.BillingAddress.LastName,
                phone = postProcessPaymentRequest.Order.BillingAddress.PhoneNumber
            };
            List<PayUProduct> products = new List<PayUProduct>();
            foreach (var prod in postProcessPaymentRequest.Order.OrderItems)
            {
                PayUProduct p = new PayUProduct();
                p.name = prod.Product.Name;
                p.quantity = prod.Quantity;
                p.unitPrice = (int)(prod.Product.Price * 100);
                products.Add(p);
            }
            order.products = products;
            request.AddParameter("application/json; charset=utf-8", request.JsonSerializer.Serialize(order), ParameterType.RequestBody);
            var securityClient = new RestClient(this._PayuPaymentSettings.BaseUrl.TrimEnd('/')  + "/pl/standard/user/oauth");
            var securityRequest = new RestRequest("authorize", Method.POST);
            securityRequest.AddParameter("grant_type", "client_credentials");
            securityRequest.AddParameter("client_id", this._PayuPaymentSettings.PosId);
            securityRequest.AddParameter("client_secret", this._PayuPaymentSettings.OAuthClientSecret);
            var response = securityClient.Execute<PayUAuthorizationResponse>(securityRequest);
            var accToken = response.Data.Access_token;
            request.AddHeader("Authorization", "Bearer " + accToken);
            var orderResponse = cl.Post<PayUOrderResponse>(request);
            HttpContext.Current.Response.Redirect(orderResponse.Data.redirectUri);
            HttpContext.Current.Response.Flush();
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            CapturePaymentResult result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            RefundPaymentResult result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            VoidPaymentResult result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            ProcessPaymentResult result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            CancelRecurringPaymentResult result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException("order");
            }
            return order.PaymentStatus == Core.Domain.Payments.PaymentStatus.Pending && (DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes >= 1.0;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentPayu";
            routeValues = new RouteValueDictionary
            {
                {
                    "Namespaces",
                    "Nop.Plugin.Payments.Payu.Controllers"
                },
                {
                    "area",
                    null
                }
            };
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentPayu";
            routeValues = new RouteValueDictionary
            {
                {
                    "Namespaces",
                    "Nop.Plugin.Payments.Payu.Controllers"
                },
                {
                    "area",
                    null
                }
            };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentPayuController);
        }

        public override void Install()
        {
            PayuPaymentSettings settings = new PayuPaymentSettings
            {
                PosId = "",
                OAuthClientSecret = "",
                SecondKey = "",
                BaseUrl = "",
                OAuthClientId = ""
            };
            this._settingService.SaveSetting<PayuPaymentSettings>(settings, 0);
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.RedirectionTip", "You will be redirected to Payu site to complete the order.");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.PosId", "PoS ID");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.PosId.Hint", "Enter PoS ID provided by PayU.");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.OAuthClientId", "OAuth Client ID");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.OAuthClientId.Hint", "Enter OAuth Client ID provided by PayU.");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.SecondKey", "Second Key(MD5)");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.SecondKey.Hint", "Enter Second Key(MD5) provided by PayU.");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.BaseUrl", "PayU payment service base URL");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.BaseUrl.Hint", "Enter PayU payment service base URL.");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.OAuthClientSecret", "OAuth Client Secret");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.OAuthClientSecret.Hint", "Enter OAuth Client Secret provided by PayU.");
            base.Install();
        }

        public override void Uninstall()
        {
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.RedirectionTip");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.PosId");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.PosId.Hint");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.OAuthClientId");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.OAuthClientId.Hint");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.SecondKey");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.SecondKey.Hint");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.BaseUrl");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.BaseUrl.Hint");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.OAuthClientSecret");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.OAuthClientSecret.Hint");
            base.Uninstall();
        }
    }
}
