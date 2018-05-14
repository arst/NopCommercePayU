using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.PayU.Infrastructure;
using Nop.Plugin.Payments.PayU.Integration.Payment;
using Nop.Plugin.Payments.PayU.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.Payu.Controllers
{
    public class PaymentPayuController : BasePaymentController
    {
        private readonly ISettingService _settingService;

        private readonly IPaymentService _paymentService;

        private readonly IOrderService _orderService;

        private readonly IOrderProcessingService _orderProcessingService;

        private readonly PayuPaymentSettings _PayuPaymentSettings;

        private readonly PaymentSettings _paymentSettings;

        private readonly ILogger _logger;

        public PaymentPayuController(ISettingService settingService, IPaymentService paymentService, IOrderService orderService, IOrderProcessingService orderProcessingService, PayuPaymentSettings PayuPaymentSettings, PaymentSettings paymentSettings, ILogger _logger)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._PayuPaymentSettings = PayuPaymentSettings;
            this._paymentSettings = paymentSettings;
            this._logger = _logger;
        }

        [AdminAuthorize, ChildActionOnly]
        public ActionResult Configure()
        {
            return base.View("~/Plugins/Payments.Payu/Views/PaymentPayu/Configure.cshtml", new ConfigurationModel
            {
                PosId = this._PayuPaymentSettings.PosId,
                OAuthClientSecret = this._PayuPaymentSettings.OAuthClientSecret,
                OAuthClientId = this._PayuPaymentSettings.OAuthClientId,
                BaseUrl = this._PayuPaymentSettings.BaseUrl,
                SecondKey = this._PayuPaymentSettings.SecondKey,
                AdditionalFee = this._PayuPaymentSettings.AdditionalFee
            });
        }

        [AdminAuthorize, ChildActionOnly, HttpPost]
        public ActionResult Configure(ConfigurationModel model)
        {
            ActionResult result;
            if (!base.ModelState.IsValid)
            {
                result = this.Configure();
            }
            else
            {
                this._PayuPaymentSettings.PosId = model.PosId;
                this._PayuPaymentSettings.OAuthClientSecret = model.OAuthClientSecret;
                this._PayuPaymentSettings.OAuthClientId = model.OAuthClientId;
                this._PayuPaymentSettings.BaseUrl = model.BaseUrl;
                this._PayuPaymentSettings.SecondKey = model.SecondKey;
                this._PayuPaymentSettings.AdditionalFee = model.AdditionalFee;
                this._settingService.SaveSetting<PayuPaymentSettings>(this._PayuPaymentSettings, 0);
                result = base.View("~/Plugins/Payments.Payu/Views/PaymentPayu/Configure.cshtml", model);
            }
            return result;
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return base.View("~/Plugins/Payments.Payu/Views/PaymentPayu/PaymentInfo.cshtml");
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            return new List<string>();
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        [ValidateInput(false)]
        public async Task<HttpStatusCodeResult> Return(PayUOrderNotification notification)
        {
            PayuPaymentProcessor processor = this._paymentService.LoadPaymentMethodBySystemName("Payments.Payu") as PayuPaymentProcessor;

            var signature = this.ExtractPayUSignature(Request.Headers);
           
            await this.VerifyRequest(Request, signature);

            if (processor == null || 
                !PaymentExtensions.IsPaymentMethodActive(processor, this._paymentSettings) || 
                !processor.PluginDescriptor.Installed)
            {
                throw new NopException("Payu payments module cannot be loaded");
            }

            if (String.IsNullOrEmpty(this._PayuPaymentSettings.OAuthClientSecret))
            {
                throw new NopException("Payu client secret can't be null or empty");
            }
            string merchantId = this._PayuPaymentSettings.PosId;
            int localOrderNumber = Convert.ToInt32(notification.Order.ExtOrderId);
            Order order = this._orderService.GetOrderById(localOrderNumber);

            switch (notification.Order.Status)
            {
                case "COMPLETED":
                    if (this._orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        order.AuthorizationTransactionId = notification.Order.OrderId;
                        this._orderService.UpdateOrder(order);
                        this._orderProcessingService.MarkOrderAsPaid(order);
                    }
                    break;
                case "REJECTED":
                case "CANCELED":
                    if (this._orderProcessingService.CanCancelOrder(order))
                    {
                        order.AuthorizationTransactionId = notification.Order.OrderId;
                        this._orderService.UpdateOrder(order);
                        this._orderProcessingService.CancelOrder(order, true);
                    }
                    break;
                default:
                    break;
            }

            return new HttpStatusCodeResult(200);
        }

        private async Task VerifyRequest(HttpRequestBase request, string signature)
        {
            var requestBody = await Request.GetBody();
            var verificationString = String.Concat(requestBody, _PayuPaymentSettings.SecondKey);

            using (MD5 md5Hash = MD5.Create())
            {
                if (!MD5HashManager.VerifyMd5Hash(md5Hash, verificationString, signature))
                {
                    throw new SecurityException("Signatures don't match.");
                }
            }
        }

        private string ExtractPayUSignature(NameValueCollection headers)
        {
            const int signaturePosition = 2;
            var match = Regex.Match(Request.Headers.Get("OpenPayu-Signature"), "(signature=)([A-z,0-9]*)");
            var signature = match.Groups[signaturePosition].Value;

            return signature;
        }
    }
}
