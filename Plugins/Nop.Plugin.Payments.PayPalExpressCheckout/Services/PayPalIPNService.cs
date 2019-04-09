﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.PayPalExpressCheckout.Services
{
    public class PayPalIPNService : IPayPalIPNService
    {
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly PayPalExpressCheckoutPaymentSettings _payPalExpressCheckoutPaymentSettings;
        private readonly ILogger _logger;

        public PayPalIPNService(IOrderService orderService, IOrderProcessingService orderProcessingService, PayPalExpressCheckoutPaymentSettings payPalExpressCheckoutPaymentSettings, ILogger logger)
        {
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _payPalExpressCheckoutPaymentSettings = payPalExpressCheckoutPaymentSettings;
            _logger = logger;
        }

        public void HandleIPN(string ipnData)
        {
            if (VerifyIPN(ipnData, out Dictionary<string, string> values))
            {
                
                var total = decimal.Zero;
                try
                {
                    total = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
                }
                catch { }

                values.TryGetValue("payer_status", out string _);
                values.TryGetValue("payment_status", out string paymentStatus);
                values.TryGetValue("pending_reason", out string pendingReason);
                values.TryGetValue("mc_currency", out string _);
                values.TryGetValue("txn_id", out string _);
                values.TryGetValue("txn_type", out string txnType);
                values.TryGetValue("rp_invoice_id", out string rpInvoiceID);
                values.TryGetValue("payment_type", out string _);
                values.TryGetValue("payer_id", out string _);
                values.TryGetValue("receiver_id", out string _);
                values.TryGetValue("invoice", out string _);
                values.TryGetValue("payment_fee", out string _);


                var sb = new StringBuilder();
                sb.AppendLine("Paypal IPN:");
                foreach (var kvp in values)
                {
                    sb.AppendLine(kvp.Key + ": " + kvp.Value);
                }

                var newPaymentStatus = GetPaymentStatus(paymentStatus, pendingReason);
                sb.AppendLine("New payment status: " + newPaymentStatus);

                switch (txnType)
                {
                    case "recurring_payment_profile_created":
                        //do nothing here
                        break;
                    case "recurring_payment":
                        {
                            var orderNumberGuid = Guid.Empty;
                            try
                            {
                                orderNumberGuid = new Guid(rpInvoiceID);
                            }
                            catch
                            {
                            }

                            var initialOrder = _orderService.GetOrderByGuid(orderNumberGuid);
                            if (initialOrder != null)
                            {
                                var recurringPayments = _orderService.SearchRecurringPayments(0, 0, initialOrder.Id, null, 0, int.MaxValue);
                                foreach (var rp in recurringPayments)
                                {
                                    switch (newPaymentStatus)
                                    {
                                        case PaymentStatus.Authorized:
                                        case PaymentStatus.Paid:
                                            {
                                                var recurringPaymentHistory = rp.RecurringPaymentHistory;
                                                if (recurringPaymentHistory.Count == 0)
                                                {
                                                    //first payment
                                                    var rph = new RecurringPaymentHistory()
                                                    {
                                                        RecurringPaymentId = rp.Id,
                                                        OrderId = initialOrder.Id,
                                                        CreatedOnUtc = DateTime.UtcNow
                                                    };
                                                    rp.RecurringPaymentHistory.Add(rph);
                                                    _orderService.UpdateRecurringPayment(rp);
                                                }
                                                else
                                                {
                                                    //next payments
                                                    _orderProcessingService.ProcessNextRecurringPayment(rp);
                                                }
                                            }
                                            break;
                                    }
                                }

                                //this.OrderService.InsertOrderNote(newOrder.OrderId, sb.ToString(), DateTime.UtcNow);
                                _logger.Information("PayPal IPN. Recurring info", new NopException(sb.ToString()));
                            }
                            else
                            {
                                _logger.Error("PayPal IPN. Order is not found", new NopException(sb.ToString()));
                            }
                        }
                        break;
                    default:
                        {
                            values.TryGetValue("custom", out string orderNumber);
                            var orderNumberGuid = Guid.Empty;
                            try
                            {
                                orderNumberGuid = new Guid(orderNumber);
                            }
                            catch
                            {
                            }

                            var order = _orderService.GetOrderByGuid(orderNumberGuid);
                            if (order != null)
                            {

                                //order note
                                order.OrderNotes.Add(new OrderNote
                                {
                                    Note = sb.ToString(),
                                    DisplayToCustomer = false,
                                    CreatedOnUtc = DateTime.UtcNow
                                });
                                _orderService.UpdateOrder(order);

                                switch (newPaymentStatus)
                                {
                                    case PaymentStatus.Pending:
                                        {
                                        }
                                        break;
                                    case PaymentStatus.Authorized:
                                        {
                                            if (_orderProcessingService.CanMarkOrderAsAuthorized(order))
                                            {
                                                _orderProcessingService.MarkAsAuthorized(order);
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Paid:
                                        {
                                            if (_orderProcessingService.CanMarkOrderAsPaid(order))
                                            {
                                                _orderProcessingService.MarkOrderAsPaid(order);
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Refunded:
                                        {
                                            if (_orderProcessingService.CanRefundOffline(order))
                                            {
                                                _orderProcessingService.RefundOffline(order);
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Voided:
                                        {
                                            if (_orderProcessingService.CanVoidOffline(order))
                                            {
                                                _orderProcessingService.VoidOffline(order);
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                _logger.Error("PayPal IPN. Order is not found", new NopException(sb.ToString()));
                            }
                        }
                        break;
                }
            }
            else
            {
                _logger.Error("PayPal IPN failed.", new NopException(ipnData));
            }
        }
        /// <summary>
        /// Verifies IPN
        /// </summary>
        /// <param name="ipnData">IPN data</param>
        /// <param name="values">Values</param>
        /// <returns>Result</returns>
        public bool VerifyIPN(string ipnData, out Dictionary<string, string> values)
        {
            var req = (HttpWebRequest)WebRequest.Create(GetPaypalUrl());
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            var formContent = $"{ipnData}&cmd=_notify-validate";
            req.ContentLength = formContent.Length;

            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
            {
                sw.Write(formContent);
            }

            string response;
            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                response = WebUtility.UrlDecode(sr.ReadToEnd());
            }
            var success = response.Trim().Equals("VERIFIED", StringComparison.OrdinalIgnoreCase);

            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in ipnData.Split('&'))
            {
                var line = l.Trim();
                var equalPox = line.IndexOf('=');
                if (equalPox >= 0)
                    values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
            }

            return success;
        }
        /// <summary>
        /// Gets Paypal URL
        /// </summary>
        /// <returns></returns>
        private string GetPaypalUrl()
        {
            return _payPalExpressCheckoutPaymentSettings.IsLive ? "https://www.paypal.com/us/cgi-bin/webscr" :
                       "https://www.sandbox.paypal.com/us/cgi-bin/webscr";
        }
        /// <summary>
        /// Gets a payment status
        /// </summary>
        /// <param name="paymentStatus">PayPal payment status</param>
        /// <param name="pendingReason">PayPal pending reason</param>
        /// <returns>Payment status</returns>
        private static PaymentStatus GetPaymentStatus(string paymentStatus, string pendingReason)
        {
            var result = PaymentStatus.Pending;

            if (paymentStatus == null)
                paymentStatus = string.Empty;

            if (pendingReason == null)
                pendingReason = string.Empty;

            switch (paymentStatus.ToLowerInvariant())
            {
                case "pending":
                    switch (pendingReason.ToLowerInvariant())
                    {
                        case "authorization":
                            result = PaymentStatus.Authorized;
                            break;
                        default:
                            result = PaymentStatus.Pending;
                            break;
                    }
                    break;
                case "processed":
                case "completed":
                case "canceled_reversal":
                    result = PaymentStatus.Paid;
                    break;
                case "denied":
                case "expired":
                case "failed":
                case "voided":
                    result = PaymentStatus.Voided;
                    break;
                case "refunded":
                case "reversed":
                    result = PaymentStatus.Refunded;
                    break;
                default:
                    break;
            }
            return result;
        }
    }
}