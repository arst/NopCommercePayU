﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.PayuRedirect.Infrastructure;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models.Payment;
using Nop.Plugin.Payments.PayuRedirect.Models;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.PayuRedirect.Controllers
{
    public class PaymentPayuController : BasePaymentController
    {
        private readonly ISettingService settingService;

        private readonly IPaymentService paymentService;

        private readonly IOrderService orderService;

        private readonly IOrderProcessingService orderProcessingService;

        private readonly PayuPaymentSettings payuPaymentSettings;

        private readonly PaymentSettings paymentSettings;

        private readonly ILogger logger;

        public PaymentPayuController(ISettingService settingService, IPaymentService paymentService, IOrderService orderService, IOrderProcessingService orderProcessingService, PayuPaymentSettings PayuPaymentSettings, PaymentSettings paymentSettings, ILogger logger)
        {
            this.settingService = settingService;
            this.paymentService = paymentService;
            this.orderService = orderService;
            this.orderProcessingService = orderProcessingService;
            this.payuPaymentSettings = PayuPaymentSettings;
            this.paymentSettings = paymentSettings;
            this.logger = logger;
        }

        [AdminAuthorize, ChildActionOnly]
        public ActionResult Configure()
        {
            return base.View("~/Plugins/Payments.Payu/Views/PaymentPayu/Configure.cshtml", new ConfigurationModel
            {
                PosId = this.payuPaymentSettings.PosId,
                OAuthClientSecret = this.payuPaymentSettings.OAuthClientSecret,
                OAuthClientId = this.payuPaymentSettings.OAuthClientId,
                BaseUrl = this.payuPaymentSettings.BaseUrl,
                SecondKey = this.payuPaymentSettings.SecondKey,
                AdditionalFee = this.payuPaymentSettings.AdditionalFee,
                TransactModeId = payuPaymentSettings.TransactMode,
                TransactModeValues = payuPaymentSettings.TransactMode.ToSelectList(),
                Currency = payuPaymentSettings.Currency
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
                this.payuPaymentSettings.PosId = model.PosId;
                this.payuPaymentSettings.OAuthClientSecret = model.OAuthClientSecret;
                this.payuPaymentSettings.OAuthClientId = model.OAuthClientId;
                this.payuPaymentSettings.BaseUrl = model.BaseUrl;
                this.payuPaymentSettings.SecondKey = model.SecondKey;
                this.payuPaymentSettings.AdditionalFee = model.AdditionalFee;
                this.payuPaymentSettings.TransactMode = model.TransactModeId;
                this.payuPaymentSettings.Currency = model.Currency;
                this.settingService.SaveSetting(this.payuPaymentSettings, 0);
                model.TransactModeValues = model.TransactModeId.ToSelectList();
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
        public async Task<HttpStatusCodeResult> Return(PayuOrderNotification notification)
        {
            PayuPaymentProcessor processor = this.paymentService.LoadPaymentMethodBySystemName("Payments.Payu") as PayuPaymentProcessor;

            var signature = this.ExtractPayUSignature(Request.Headers);

            await this.VerifyRequest(Request, signature);

            if (processor == null ||
                !PaymentExtensions.IsPaymentMethodActive(processor, this.paymentSettings) ||
                !processor.PluginDescriptor.Installed)
            {
                throw new NopException("Payu payments module cannot be loaded");
            }

            if (String.IsNullOrEmpty(this.payuPaymentSettings.OAuthClientSecret))
            {
                throw new NopException("Payu client secret can't be null or empty");
            }
            var merchantId = this.payuPaymentSettings.PosId;
            var localOrderNumber = Convert.ToInt32(notification.Order.ExtOrderId);
            var order = this.orderService.GetOrderById(localOrderNumber);

            switch (notification.Order.Status)
            {
                case PayuOrderStatusCode.Completed:
                    if (this.orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        order.AuthorizationTransactionId = notification.Order.OrderId;
                        this.orderService.UpdateOrder(order);
                        this.orderProcessingService.MarkOrderAsPaid(order);
                    }
                    break;
                case PayuOrderStatusCode.Pending:
                    order.PaymentStatus = PaymentStatus.Pending;
                    this.orderService.UpdateOrder(order);
                    break;
                case PayuOrderStatusCode.WaitingForConfirmation:
                    if (this.orderProcessingService.CanMarkOrderAsAuthorized(order))
                    {
                        this.orderProcessingService.MarkAsAuthorized(order);
                    }
                    break;
                case PayuOrderStatusCode.Rejected:
                case PayuOrderStatusCode.Canceled:
                    if (this.orderProcessingService.CanCancelOrder(order))
                    {
                        order.AuthorizationTransactionId = notification.Order.OrderId;
                        this.orderService.UpdateOrder(order);
                        this.orderProcessingService.CancelOrder(order, true);
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
            var verificationString = String.Concat(requestBody, payuPaymentSettings.SecondKey);

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
