namespace Nop.Plugin.Payments.Payu
{
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Web;
    using System.Web.Routing;
    using Nop.Core;
    using Nop.Core.Domain.Directory;
    using Nop.Core.Domain.Orders;
    using Nop.Core.Domain.Payments;
    using Nop.Core.Infrastructure;
    using Nop.Core.Plugins;
    using Nop.Plugin.Payments.Payu.Controllers;
    using Nop.Plugin.Payments.PayU.Api.Refund;
    using Nop.Plugin.Payments.PayU.Integration;
    using Nop.Plugin.Payments.PayU.Integration.Authorization;
    using Nop.Plugin.Payments.PayU.Integration.Capture;
    using Nop.Plugin.Payments.PayU.Integration.Payment;
    using Nop.Plugin.Payments.PayU.Integration.Refund;
    using Nop.Services.Configuration;
    using Nop.Services.Directory;
    using Nop.Services.Localization;
    using Nop.Services.Logging;
    using Nop.Services.Payments;
    using RestSharp;

    public class PayuPaymentProcessor : BasePlugin, IPaymentMethod, IPlugin
    {
        private readonly PayuPaymentSettings payuPaymentSettings;

        private readonly ISettingService settingService;

        private readonly ICurrencyService currencyService;

        private readonly CurrencySettings currencySettings;

        private readonly IWebHelper webHelper;

        private readonly ILogger logger;

        public bool SupportCapture
        {
            get
            {
                return true;
            }
        }

        public bool SupportPartiallyRefund
        {
            get
            {
                return true;
            }
        }

        public bool SupportRefund
        {
            get
            {
                return true;
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
                return RecurringPaymentType.NotSupported;
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

        public PayuPaymentProcessor(PayuPaymentSettings PayuPaymentSettings, ISettingService settingService, ICurrencyService currencyService, CurrencySettings currencySettings, IWebHelper webHelper, ILogger _logger)
        {
            this.payuPaymentSettings = PayuPaymentSettings;
            this.settingService = settingService;
            this.currencyService = currencyService;
            this.currencySettings = currencySettings;
            this.webHelper = webHelper;
            this.logger = _logger;
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            ProcessPaymentResult result = 
                new ProcessPaymentResult
                {
                    NewPaymentStatus = PaymentStatus.Pending
                };

            return result;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            this.ValidatePaymentSettings();
            var payUApiClient = GetApiClient("api/v2_1");
            var request = new RestRequest("orders", Method.POST);
            request.AddHeader("Content-Type", "application/json");
            var payuOrder = PreparePayuOrder(postProcessPaymentRequest);
            request.AddParameter("application/json; charset=utf-8", request.JsonSerializer.Serialize(payuOrder), ParameterType.RequestBody);
            var authenticationToken = GetAuthToken();
            request.AddHeader("Authorization", String.Concat("Bearer ", authenticationToken));
            var orderResponse = payUApiClient.Post<PayUOrderResponse>(request);
            HttpContext.Current.Response.Redirect(orderResponse.Data.RedirectUri);
            HttpContext.Current.Response.Flush();
        }

        private PayUOrder PreparePayuOrder(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            PayUOrder order = new PayUOrder();
            order.CurrencyCode = currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId).CurrencyCode;
            order.CustomerIp = webHelper.GetCurrentIpAddress();
            
            order.Description = String.Concat("Order from ", GetStoreName());
            order.ExtOrderId = postProcessPaymentRequest.Order.Id.ToString();
            order.MerchantPosId = payuPaymentSettings.PosId;
            order.NotifyUrl = webHelper.GetStoreLocation(false) + "Plugins/PaymentPayu/Return";
            order.TotalAmount = (int)(postProcessPaymentRequest.Order.OrderTotal * 100);
            order.Buyer = new PayUBuyer()
            {
                Email = postProcessPaymentRequest.Order.BillingAddress.Email,
                FirstName = postProcessPaymentRequest.Order.BillingAddress.FirstName,
                LastName = postProcessPaymentRequest.Order.BillingAddress.LastName,
                Phone = postProcessPaymentRequest.Order.BillingAddress.PhoneNumber
            };
            List<PayUProduct> products = new List<PayUProduct>();
            foreach (var prod in postProcessPaymentRequest.Order.OrderItems)
            {
                PayUProduct p = new PayUProduct();
                p.Name = prod.Product.Name;
                p.Quantity = prod.Quantity;
                p.UnitPrice = (int)(prod.Product.Price * 100);
                products.Add(p);
            }
            order.Products = products;

            return order;
        }

        private object GetStoreName()
        {
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var storeName = storeContext.CurrentStore.Name;

            return storeName;
        }

        private RestClient GetApiClient(string relativePath)
        {
            var baseUri = new Uri(payuPaymentSettings.BaseUrl);
            var relativeUri = new Uri(baseUri, relativePath);
            RestClient client = new RestClient(relativeUri);
            client.FollowRedirects = false;

            return client;
        }

        private void ValidatePaymentSettings()
        {
            if (String.IsNullOrEmpty(payuPaymentSettings.BaseUrl))
            {
                throw new ArgumentNullException("You must setup base url for PayU before use this payment method");
            }

            if (String.IsNullOrEmpty(payuPaymentSettings.OAuthClientSecret))
            {
                throw new ArgumentNullException("You must setup oauth client secret before using this payment method");
            }

            if (String.IsNullOrEmpty(payuPaymentSettings.OAuthClientId))
            {
                throw new ArgumentNullException("You must setup oauth client id before using this payment method");
            }


            if (String.IsNullOrEmpty(payuPaymentSettings.PosId))
            {
                throw new ArgumentNullException("You must setup PoS ID before using this payment method");
            }

            if (String.IsNullOrEmpty(payuPaymentSettings.SecondKey))
            {
                throw new ArgumentNullException("You must setup second key(md5) before using this payment method");
            }
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return payuPaymentSettings.AdditionalFee;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            CapturePaymentResult result = new CapturePaymentResult();
            CaptureOrderRequest request = new CaptureOrderRequest();
            request.OrderId = capturePaymentRequest.Order.AuthorizationTransactionId;
            request.OrderStatus = PayuApiOrderStatusCode.Completed;

            var payUApiClient = GetApiClient("api/v2_1");
            var captureRequest = new RestRequest(String.Format("orders/{0}/status", request.OrderId), Method.PUT);
            captureRequest.AddHeader("Content-Type", "application/json");
            captureRequest.AddParameter("application/json; charset=utf-8", captureRequest.JsonSerializer.Serialize(request), ParameterType.RequestBody);
            var authenticationToken = GetAuthToken();
            captureRequest.AddHeader("Authorization", String.Concat("Bearer ", authenticationToken));

            var orderResponse = payUApiClient.Put<CaptureOrderResponse>(captureRequest);

            if (orderResponse.Data.StatusCode.Equals(PayuApiResponseStatusCode.Success, StringComparison.OrdinalIgnoreCase))
            {
                capturePaymentRequest.Order.CaptureTransactionResult = orderResponse.Data.StatusCode;
                result.NewPaymentStatus = PaymentStatus.Paid;
            }
            else
            {
                result.AddError(orderResponse.Data.StatusDesc);
            }

            return result;
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            RefundPaymentResult result = new RefundPaymentResult();

            RestClient payuApiClient = GetApiClient(String.Format("/api/v2_1/orders/{0}/refunds", refundPaymentRequest.Order.AuthorizationTransactionId));
            RestRequest refundRequest = new RestRequest();
            RefundRequest refund = new RefundRequest();

            if (refundPaymentRequest.IsPartialRefund)
            {
                refund.Amount = refundPaymentRequest.AmountToRefund.ToString();
            }
            refundRequest.AddParameter("application/json; charset=utf-8", refundRequest.JsonSerializer.Serialize(refund), ParameterType.RequestBody);
            refundRequest.AddHeader("Authorization", String.Concat("Bearer ", GetAuthToken()));
            var refundResult = payuApiClient.Execute<RefundResponse>(refundRequest);

            if (refundResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.Error("Can't process refund with PayU payment, reason: " + refundResult.Data.Status.StatusCode);
                result.Errors.Add(refundResult.Data.Status.StatusDescription);
            }
            else
            {
                result.NewPaymentStatus = PaymentStatus.Refunded;
            }

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
            return order.PaymentStatus == PaymentStatus.Pending && (DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes >= 1.0;
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
            this.settingService.SaveSetting<PayuPaymentSettings>(settings, 0);
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.PayU.Instructions", "");
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
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.AdditionalFee", "Additional Fee");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.AdditionalFee.Hint", "Enter Additional.");
            base.Install();
        }

        public override void Uninstall()
        {
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.PayU.Instructions");
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
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.AdditionalFee");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.AdditionalFee.Hint");
            base.Uninstall();
        }

        private string GetAuthToken()
        {
            var securityClient = GetApiClient("/pl/standard/user/oauth");
            var securityRequest = new RestRequest("authorize", Method.POST);
            securityRequest.AddParameter("grant_type", "client_credentials");
            securityRequest.AddParameter("client_id", payuPaymentSettings.PosId);
            securityRequest.AddParameter("client_secret", payuPaymentSettings.OAuthClientSecret);
            var response = securityClient.Execute<PayUAuthorizationResponse>(securityRequest);
            var accToken = response.Data.AccessToken;
            if (string.IsNullOrEmpty(accToken))
            {
                throw new SecurityException("PayU can't generate bearer token. Check payment method setting or contact responsible person.");
            }

            return accToken;
        }
    }
}
