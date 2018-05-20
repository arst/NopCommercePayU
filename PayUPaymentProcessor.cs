using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Infrastructure;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.Payu.Controllers;
using Nop.Plugin.Payments.PayU;
using Nop.Plugin.Payments.PayU.Integration.Models;
using Nop.Plugin.Payments.PayU.Integration.Services;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;

namespace Nop.Plugin.Payments.Payu
{
    public class PayuPaymentProcessor : BasePlugin, IPaymentMethod, IPlugin
    {
        private readonly PayuPaymentSettings payuPaymentSettings;

        private readonly ISettingService settingService;

        private readonly ICurrencyService currencyService;

        private readonly CurrencySettings currencySettings;

        private readonly IWebHelper webHelper;

        private readonly ILogger logger;

        private readonly IPayuPaymentService payuPaymentService;

        private readonly IOrderProcessingService orderProcessingService;

        public bool SupportCapture => true;

        public bool SupportPartiallyRefund => true;

        public bool SupportRefund => true;

        public bool SupportVoid => false;

        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        public bool SkipPaymentInfo => false;

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart) => false;

        public string PaymentMethodDescription => "PayU";

        public PayuPaymentProcessor(PayuPaymentSettings payuPaymentSettings, ISettingService settingService, ICurrencyService currencyService, CurrencySettings currencySettings, IWebHelper webHelper, ILogger logger, IPayuPaymentService payuPaymentService, IOrderProcessingService orderProcessingService)
        {
            this.payuPaymentSettings = payuPaymentSettings;
            this.settingService = settingService;
            this.currencyService = currencyService;
            this.currencySettings = currencySettings;
            this.webHelper = webHelper;
            this.logger = logger;
            this.payuPaymentService = payuPaymentService;
            this.orderProcessingService = orderProcessingService;
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            ProcessPaymentResult result = new ProcessPaymentResult ()
                {
                    NewPaymentStatus = PaymentStatus.Pending
                };

            return result;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (postProcessPaymentRequest == null)
                throw new ArgumentException(nameof(postProcessPaymentRequest));

            ValidatePaymentSettings();
            var payuResponse = payuPaymentService.PlaceOrder(postProcessPaymentRequest.Order, webHelper.GetCurrentIpAddress(), GetStoreName(), new Uri(webHelper.GetStoreLocation()));

            if (payuPaymentSettings.TransactMode == TransactMode.Authorize)
            {
                postProcessPaymentRequest.Order.AuthorizationTransactionId = payuResponse.OrderId;
                postProcessPaymentRequest.Order.AuthorizationTransactionResult = payuResponse.Status.StatusCode;
                orderProcessingService.MarkAsAuthorized(postProcessPaymentRequest.Order);
            }
            else if (payuPaymentSettings.TransactMode == TransactMode.AuthorizeAndCapture)
            {
                postProcessPaymentRequest.Order.CaptureTransactionId = payuResponse.OrderId;
                postProcessPaymentRequest.Order.CaptureTransactionResult = payuResponse.Status.StatusCode;
                orderProcessingService.MarkOrderAsPaid(postProcessPaymentRequest.Order);
            }

            if (!String.IsNullOrEmpty(payuResponse.RedirectUri))
            {
                HttpContext.Current.Response.Redirect(payuResponse.RedirectUri);
                HttpContext.Current.Response.Flush();
            }
            else
            {
                throw new NopException("PayU service failed to produce redirect url.");
            }
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart) => payuPaymentSettings.AdditionalFee;

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var captureResponse = payuPaymentService.CapturePayment(capturePaymentRequest.Order);

            if (!captureResponse.IsSuccess)
                throw new NopException(String.Format("Capture payment failed with error: {0}", captureResponse.Status?.StatusDesc));

            return new CapturePaymentResult()
            {
                NewPaymentStatus = PaymentStatus.Paid,
                CaptureTransactionId = capturePaymentRequest.Order.AuthorizationTransactionId
            };
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            RefundPaymentResult result = new RefundPaymentResult();
            var refundResult = payuPaymentService.RequestRefund(refundPaymentRequest.Order, refundPaymentRequest.AmountToRefund, refundPaymentRequest.IsPartialRefund);

            if (refundResult.Success)
            {
                result.NewPaymentStatus = PaymentStatus.Refunded;
            }
            else
            {
                result.Errors.Add(refundResult.Status.StatusDescription);
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

        public Type GetControllerType() => typeof(PaymentPayuController);

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
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.Currency", "Your shop currency from PayU");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.Currency.Hint", "Enter currency code from your PayU account");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.TransactionMode", "Transaction mode");
            LocalizationExtensions.AddOrUpdatePluginLocaleResource(this, "Plugins.Payments.Payu.TransactionMode.Hint", "Select transaction mode");

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
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.Currency");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.Currency.Hint");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.TransactionMode");
            LocalizationExtensions.DeletePluginLocaleResource(this, "Plugins.Payments.Payu.TransactionMode.Hint");
            
            base.Uninstall();
        }

        private string GetStoreName()
        {
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var storeName = storeContext.CurrentStore.Name;

            return storeName;
        }

        private void ValidatePaymentSettings()
        {
            if (String.IsNullOrEmpty(payuPaymentSettings.Currency))
            {
                throw new ArgumentNullException("You must setup currency for PayU before use this payment method");
            }

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
    }
}
