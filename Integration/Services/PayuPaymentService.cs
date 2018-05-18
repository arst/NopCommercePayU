using System;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Payu;
using Nop.Plugin.Payments.PayU.Infrastructure;
using Nop.Plugin.Payments.PayU.Integration.Models;
using Nop.Plugin.Payments.PayU.Integration.Models.Capture;
using Nop.Plugin.Payments.PayU.Integration.Models.Payment;
using Nop.Plugin.Payments.PayU.Integration.Models.Refund;
using Nop.Services.Directory;
using RestSharp;
using RestSharp.Serializers;

namespace Nop.Plugin.Payments.PayU.Integration.Services
{
    internal class PayuPaymentService : IPayuPaymentService
    {
        private const string NotifyRelativeUrl = "Plugins/PaymentPayu/Return";

        private readonly IPayuRestClientFactory clientFactory;
        private readonly IPayuAuthorizationService authorizationService;
        private readonly ICurrencyService currencyService;
        private readonly PayuPaymentSettings paymentSettings;

        public PayuPaymentService(IPayuRestClientFactory clientFactory, IPayuAuthorizationService authorizationService, ICurrencyService currencyService, PayuPaymentSettings payuPaymentSettings)
        {
            this.clientFactory = clientFactory;
            this.authorizationService = authorizationService;
            this.currencyService = currencyService;
            this.paymentSettings = payuPaymentSettings;
        }

        public PayuCaptureOrderResponse CapturePayment(Order order)
        {
            var request = new PayuCaptureOrderRequest();
            request.OrderId = order.AuthorizationTransactionId;
            request.OrderStatus = PayuOrderStatusCode.Completed;
            var payUApiClient = clientFactory.GetApiClient("api/v2_1");
            var captureRequest = new RestRequest(String.Format("orders/{0}/status", request.OrderId), Method.PUT);
            captureRequest.JsonSerializer = new RestSharpJsonNetSerializer();
            captureRequest.AddHeader("Content-Type", "application/json");
            captureRequest.AddParameter("application/json; charset=utf-8", captureRequest.JsonSerializer.Serialize(request), ParameterType.RequestBody);
            var authenticationToken = authorizationService.GetAuthToken();
            captureRequest.AddHeader("Authorization", String.Concat("Bearer ", authenticationToken));

            var orderResponse = payUApiClient.Put<PayuCaptureOrderResponse>(captureRequest);

            return orderResponse.Data;
        }

        public PayuRefundResponse RequestRefund(Order order, decimal refundAmount, bool isPartial)
        {
            RestClient payuApiClient = clientFactory.GetApiClient(String.Format("/api/v2_1/orders/{0}/", order.AuthorizationTransactionId));
            RestRequest apiRequest = new RestRequest("refunds", Method.POST);
            apiRequest.JsonSerializer = new RestSharpJsonNetSerializer();
            var refund = new PayuRefundRequest()
            {
                Refund = new PayuRefund()
                {
                    Amount = isPartial ? refundAmount.ToString() : null,
                    Description = "refund"
                }             
            };
            apiRequest.AddParameter("application/json; charset=utf-8", apiRequest.JsonSerializer.Serialize(refund), ParameterType.RequestBody);
            apiRequest.AddHeader("Authorization", String.Concat("Bearer ", authorizationService.GetAuthToken()));
            var apiCallResult = payuApiClient.Post<PayuRefundResponse>(apiRequest);

            return apiCallResult.Data;
        }

        public PayUOrderResponse PlaceOrder(Order order, string customerIpAddress, string storeName, Uri storeUrl)
        {
            var payUApiClient = clientFactory.GetApiClient("api/v2_1");
            var request = new RestRequest("orders", Method.POST);
            request.JsonSerializer = new RestSharpJsonNetSerializer();
            request.AddHeader("Content-Type", "application/json");
            var payuOrder = PreparePayuOrder(order, customerIpAddress, storeName, storeUrl);
            request.AddParameter("application/json; charset=utf-8", request.JsonSerializer.Serialize(payuOrder), ParameterType.RequestBody);
            var authenticationToken = authorizationService.GetAuthToken();
            request.AddHeader("Authorization", String.Concat("Bearer ", authenticationToken));
            var orderResponse = payUApiClient.Post<PayUOrderResponse>(request);

            if (orderResponse.ResponseStatus != ResponseStatus.Completed)
            {
                throw new InvalidOperationException("Payu service failed to place an order.");
            }

            return orderResponse.Data;
        }

        private PayUOrder PreparePayuOrder(Order order, string customerIpAddress, string storeName, Uri storeUrl)
        {
            PayUOrder result = new PayUOrder();
            var currencyForPayuOrder = currencyService.GetCurrencyByCode(paymentSettings.Currency);

            if (currencyForPayuOrder == null)
            {
                throw new ArgumentException("Currency for PayU must be present in the store. Please, change settings of your store and/or PayU merchant account to match currencies between PayU and your store.");
            }
            //PayU Order general info
            result.CurrencyCode = currencyForPayuOrder.CurrencyCode;
            result.CustomerIp = customerIpAddress;
            result.Description = String.Format("Order from {0}", storeName);
            result.ExtOrderId = order.Id.ToString();
            result.MerchantPosId = paymentSettings.PosId;
            result.NotifyUrl = new Uri(storeUrl, NotifyRelativeUrl).ToString();
            result.TotalAmount = (int)(order.OrderTotal * 100);
            //PayU Order buyer
            result.Buyer = new PayUBuyer()
            {
                Email = order.BillingAddress.Email,
                FirstName = order.BillingAddress.FirstName,
                LastName = order.BillingAddress.LastName,
                Phone = order.BillingAddress.PhoneNumber
            };
            //PayU Order products
            List<PayUProduct> products = new List<PayUProduct>();
            foreach (var orderItem in order.OrderItems)
            {
                PayUProduct product = new PayUProduct();
                product.Name = orderItem.Product.Name;
                product.Quantity = orderItem.Quantity;
                product.UnitPrice = (int)(orderItem.Product.Price * 100);
                products.Add(product);
            }
            result.Products = products;

            return result;
        }

        
    }
}
