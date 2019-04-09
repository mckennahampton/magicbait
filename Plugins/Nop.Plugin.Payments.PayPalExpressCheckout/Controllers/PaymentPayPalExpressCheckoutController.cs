﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.PayPalExpressCheckout.Models;
using Nop.Plugin.Payments.PayPalExpressCheckout.Services;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.PayPalExpressCheckout.Controllers
{
    public class PaymentPayPalExpressCheckoutController : BasePaymentController
    {
        #region Fields

        private readonly IPayPalExpressCheckoutConfirmOrderService _payPalExpressCheckoutConfirmOrderService;
        private readonly IPayPalExpressCheckoutPlaceOrderService _payPalExpressCheckoutPlaceOrderService;
        private readonly IPayPalExpressCheckoutService _payPalExpressCheckoutService;
        private readonly IPayPalExpressCheckoutShippingMethodService _payPalExpressCheckoutShippingMethodService;
        private readonly IPayPalIPNService _payPalIPNService;
        private readonly IPayPalRedirectionService _payPalRedirectionService;
        private readonly ISettingService _settingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly PayPalExpressCheckoutPaymentSettings _payPalExpressCheckoutPaymentSettings;

        #endregion

        #region Ctor

        public PaymentPayPalExpressCheckoutController(IPayPalExpressCheckoutConfirmOrderService payPalExpressCheckoutConfirmOrderService,
            IPayPalExpressCheckoutPlaceOrderService payPalExpressCheckoutPlaceOrderService,
            IPayPalExpressCheckoutService payPalExpressCheckoutService,
            IPayPalExpressCheckoutShippingMethodService payPalExpressCheckoutShippingMethodService,
            IPayPalIPNService payPalIPNService,
            IPayPalRedirectionService payPalRedirectionService,
            ISettingService settingService,
            IShoppingCartService shoppingCartService,
            PayPalExpressCheckoutPaymentSettings payPalExpressCheckoutPaymentSettings)
        {
            _payPalExpressCheckoutConfirmOrderService = payPalExpressCheckoutConfirmOrderService;
            _payPalExpressCheckoutPlaceOrderService = payPalExpressCheckoutPlaceOrderService;
            _payPalExpressCheckoutService = payPalExpressCheckoutService;
            _payPalExpressCheckoutShippingMethodService = payPalExpressCheckoutShippingMethodService;
            _payPalIPNService = payPalIPNService;
            _payPalRedirectionService = payPalRedirectionService;
            _settingService = settingService;
            _shoppingCartService = shoppingCartService;
            _payPalExpressCheckoutPaymentSettings = payPalExpressCheckoutPaymentSettings;
        }

        #endregion

        #region Utilities 

        /// <summary>
        /// Check that logo image is valid
        /// </summary>
        /// <param name="logoImageUrl">URL</param>
        /// <param name="validationErrors">Errors</param>
        /// <returns>True if logo image is valid; otherwise false</returns>
        protected bool IsLogoImageValid(string logoImageUrl, out List<string> validationErrors)
        {
            validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(logoImageUrl))
                return true;

            Uri result;
            if (!Uri.TryCreate(logoImageUrl, UriKind.Absolute, out result))
            {
                validationErrors.Add("Logo Image URL is not in a valid format");
                return false;
            }

            if (result.Scheme != "https")
            {
                validationErrors.Add("Logo Image must be hosted on https");
                return false;
            }

            try
            {
                using (var imageStream = HttpWebRequest.Create(result).GetResponse().GetResponseStream())
                using (var bitmap = new Bitmap(imageStream))
                {
                    if (bitmap.Width > 190)
                        validationErrors.Add("Image must be less than or equal to 190 px in width");
                    if (bitmap.Height > 60)
                        validationErrors.Add("Image must be less than or equal to 60 px in height");
                    return !validationErrors.Any();
                }
            }
            catch
            {
                validationErrors.Add("Logo image was not a valid ");
                return false;
            }
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            var model = new ConfigurationModel
                            {
                                ApiSignature = _payPalExpressCheckoutPaymentSettings.ApiSignature,
                                LogoImageURL = _payPalExpressCheckoutPaymentSettings.LogoImageURL,
                                CartBorderColor = _payPalExpressCheckoutPaymentSettings.CartBorderColor,
                                DoNotHaveBusinessAccount = _payPalExpressCheckoutPaymentSettings.DoNotHaveBusinessAccount,
                                EmailAddress = _payPalExpressCheckoutPaymentSettings.EmailAddress,
                                EnableDebugLogging = _payPalExpressCheckoutPaymentSettings.EnableDebugLogging,
                                IsLive = _payPalExpressCheckoutPaymentSettings.IsLive,
                                Password = _payPalExpressCheckoutPaymentSettings.Password,
                                Username = _payPalExpressCheckoutPaymentSettings.Username,
                                AdditionalFee = _payPalExpressCheckoutPaymentSettings.AdditionalFee,
                                AdditionalFeePercentage = _payPalExpressCheckoutPaymentSettings.AdditionalFeePercentage,
                                LocaleCode = _payPalExpressCheckoutPaymentSettings.LocaleCode,
                                PaymentAction = _payPalExpressCheckoutPaymentSettings.PaymentAction,
                                RequireConfirmedShippingAddress = _payPalExpressCheckoutPaymentSettings.RequireConfirmedShippingAddress,
                                PaymentActionOptions = _payPalExpressCheckoutService.GetPaymentActionOptions(_payPalExpressCheckoutPaymentSettings.PaymentAction),
                                LocaleOptions = _payPalExpressCheckoutService.GetLocaleCodeOptions(_payPalExpressCheckoutPaymentSettings.LocaleCode),
                            };

            return View("~/Plugins/Payments.PayPalExpressCheckout/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            if (IsLogoImageValid(model.LogoImageURL, out List<string> validationErrors))
            {
                _payPalExpressCheckoutPaymentSettings.ApiSignature = model.ApiSignature;
                _payPalExpressCheckoutPaymentSettings.LogoImageURL = model.LogoImageURL;
                _payPalExpressCheckoutPaymentSettings.CartBorderColor = model.CartBorderColor;
                _payPalExpressCheckoutPaymentSettings.DoNotHaveBusinessAccount = model.DoNotHaveBusinessAccount;
                _payPalExpressCheckoutPaymentSettings.EmailAddress = model.EmailAddress;
                _payPalExpressCheckoutPaymentSettings.EnableDebugLogging = model.EnableDebugLogging;
                _payPalExpressCheckoutPaymentSettings.IsLive = model.IsLive;
                _payPalExpressCheckoutPaymentSettings.Password = model.Password;
                _payPalExpressCheckoutPaymentSettings.Username = model.Username;
                _payPalExpressCheckoutPaymentSettings.AdditionalFee = model.AdditionalFee;
                _payPalExpressCheckoutPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
                _payPalExpressCheckoutPaymentSettings.LocaleCode = model.LocaleCode;
                _payPalExpressCheckoutPaymentSettings.PaymentAction = model.PaymentAction;
                _payPalExpressCheckoutPaymentSettings.RequireConfirmedShippingAddress =
                    model.RequireConfirmedShippingAddress;

                _settingService.SaveSetting(_payPalExpressCheckoutPaymentSettings);
            }
            else
            {
                foreach (var validationError in validationErrors)
                {
                    ModelState.AddModelError("", validationError);
                }
            }
            model.PaymentActionOptions = _payPalExpressCheckoutService.GetPaymentActionOptions(model.PaymentAction);
            model.LocaleOptions = _payPalExpressCheckoutService.GetLocaleCodeOptions(model.LocaleCode);

            return View("~/Plugins/Payments.PayPalExpressCheckout/Views/Configure.cshtml", model);
        }
       
        public RedirectResult SubmitButton()
        {
            var cart = _payPalExpressCheckoutService.GetCart();

            return Redirect(_payPalRedirectionService.ProcessSubmitButton(cart, TempData));
        }

        public IActionResult Return(string token)
        {
            var success = _payPalRedirectionService.ProcessReturn(token);

            if (!success)
                return RedirectToRoute("ShoppingCart");

            return RedirectToAction("SetShippingMethod");
        }

        public IActionResult SetShippingMethod()
        {
            var cart = _payPalExpressCheckoutService.GetCart();

            if (!_shoppingCartService.ShoppingCartRequiresShipping(cart))
                return RedirectToAction("Confirm");

            var model = _payPalExpressCheckoutShippingMethodService.PrepareShippingMethodModel(cart);

            return View("~/Plugins/Payments.PayPalExpressCheckout/Views/SetShippingMethod.cshtml", model);
        }

        [HttpPost, ActionName("SetShippingMethod")]
        public IActionResult SetShippingMethod(string shippingoption)
        {
            //validation
            var cart = _payPalExpressCheckoutService.GetCart();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (!_payPalExpressCheckoutService.IsAllowedToCheckout())
                return new UnauthorizedResult();

            if (!_shoppingCartService.ShoppingCartRequiresShipping(cart))
            {
                _payPalExpressCheckoutShippingMethodService.SetShippingMethodToNull();
                return RedirectToAction("Confirm");
            }

            var success = _payPalExpressCheckoutShippingMethodService.SetShippingMethod(cart, shippingoption);

            return RedirectToAction(success ? "Confirm" : "SetShippingMethod");
        }

        public IActionResult Confirm()
        {
            //validation
            var cart = _payPalExpressCheckoutService.GetCart();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (!_payPalExpressCheckoutService.IsAllowedToCheckout())
                return new UnauthorizedResult();

            //model
            var model = _payPalExpressCheckoutConfirmOrderService.PrepareConfirmOrderModel(cart);

            return View("~/Plugins/Payments.PayPalExpressCheckout/Views/Confirm.cshtml", model);
        }

        [HttpPost, ActionName("Confirm")]
        public IActionResult ConfirmOrder()
        {
            //validation
            var cart = _payPalExpressCheckoutService.GetCart();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (!_payPalExpressCheckoutService.IsAllowedToCheckout())
                return new UnauthorizedResult();

            //model
            var checkoutPlaceOrderModel = _payPalExpressCheckoutPlaceOrderService.PlaceOrder();
            if (checkoutPlaceOrderModel.RedirectToCart)
                return RedirectToRoute("ShoppingCart");

            if (checkoutPlaceOrderModel.IsRedirected)
                return Content("Redirected");

            if (checkoutPlaceOrderModel.CompletedId.HasValue)
                return RedirectToRoute("CheckoutCompleted", new { orderId = checkoutPlaceOrderModel.CompletedId });

            //If we got this far, something failed, redisplay form
            return View("~/Plugins/Payments.PayPalExpressCheckout/Views/Confirm.cshtml", checkoutPlaceOrderModel);
        }

        public IActionResult IPNHandler()
        {
            using (var ms = new MemoryStream())
            {
                Request.Body.CopyTo(ms);
                var ipnData = Encoding.ASCII.GetString(ms.ToArray());

                _payPalIPNService.HandleIPN(ipnData);
            }

            //nothing should be rendered to visitor
            return Content("");
        }

        #endregion
    }
}