using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Mvc;
using Nop.Core.Domain.Directory;
using CLF.Plugin.Shipping.FreeShipping.Domain;
using CLF.Plugin.Shipping.FreeShipping.Models;
using CLF.Plugin.Shipping.FreeShipping.Services;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Shipping;
using Nop.Web.Framework.Controllers;
using Telerik.Web.Mvc;

namespace CLF.Plugin.Shipping.FreeShipping.Controllers
{
    [AdminAuthorize]
    public class FreeShippingOrdersOverController : Controller
    {
        private readonly IShippingService _shippingService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IFreeShippingByOrderTotalService _freeShippingByOrderTotalService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IMeasureService _measureService;
        private readonly MeasureSettings _measureSettings;

        public FreeShippingOrdersOverController(IShippingService shippingService,
            ICountryService countryService, IStateProvinceService stateProvinceService,            
            IFreeShippingByOrderTotalService freeShippingByOrderTotalService, ISettingService settingService,
            ILocalizationService localizationService, 
            ICurrencyService currencyService, CurrencySettings currencySettings,
            IMeasureService measureService, MeasureSettings measureSettings)
        {
            this._shippingService = shippingService;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;          
            this._freeShippingByOrderTotalService = freeShippingByOrderTotalService;
            this._settingService = settingService;
            this._localizationService = localizationService;

            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._measureService = measureService;
            this._measureSettings = measureSettings;
        }
        
        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            //little hack here
            //always set culture to 'en-US' (Telerik has a bug related to editing decimal values in other cultures). Like currently it's done for admin area in Global.asax.cs
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            base.Initialize(requestContext);
        }

        public ActionResult Configure()
        {
                       
            return View("CLF.Plugin.Shipping.FreeShipping.Views.FreeShipping.Configure");
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult ConditionsList(GridCommand command)
        {
            var records = _freeShippingByOrderTotalService.GetAll(command.Page - 1, command.PageSize);
            var sbwModel = records.Select(x =>
                {
                    var m = new ShippingFreeOrdersOverModel()
                    {
                        Id = x.Id,                        
                        CountryId = x.CountryId,
                        OrderOver = x.OrderOver,
                        StateProvinceId = x.StateProvinceId,
                        ShippingMethodId = x.ShippingMethodId,
                        
                    };

                    var shippingMethod = _shippingService.GetShippingMethodById(x.ShippingMethodId);
                    m.ShippingMethodName = (shippingMethod != null) ? shippingMethod.Name : "Unavailable";
                    var c = _countryService.GetCountryById(x.CountryId);
                    m.CountryName = (c != null) ? c.Name : "*";
                    var s = _stateProvinceService.GetStateProvinceById(x.StateProvinceId);
                    m.StateProvinceName = (s != null) ? s.Name : "*";
                    m.Zip = (!String.IsNullOrEmpty(x.Zip)) ? x.Zip : "*";
                                      

                    return m;
                })
                .ToList();
            var model = new GridModel<ShippingFreeOrdersOverModel>
            {
                Data = sbwModel,
                Total = records.TotalCount
            };

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ConditionsDelete(int id, GridCommand command)
        {
            var sbw = _freeShippingByOrderTotalService.GetById(id);
            if (sbw != null)
                _freeShippingByOrderTotalService.DeleteFreeShippingRecord(sbw);

            return ConditionsList(command);
        }
              

        //add
        public ActionResult AddPopup()
        {
            var model = new ShippingFreeOrdersOverModel();
            model.PrimaryStoreCurrencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode;

            var shippingMethods = _shippingService.GetAllShippingMethods();
            if (shippingMethods.Count == 0)
                return Content("No shipping methods can be loaded");

            //shipping methods
            foreach (var sm in shippingMethods)
                model.AvailableShippingMethods.Add(new SelectListItem() { Text = sm.Name, Value = sm.Id.ToString() });

            //countries
            model.AvailableCountries.Add(new SelectListItem() { Text = "*", Value = "0" });
            var countries = _countryService.GetAllCountries(true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });
            //states
            model.AvailableStates.Add(new SelectListItem() { Text = "*", Value = "0" });

            return View("CLF.Plugin.Shipping.FreeShipping.Views.FreeShipping.AddPopup", model);
        }
        [HttpPost]
        public ActionResult AddPopup(string btnId, string formId, ShippingFreeOrdersOverModel model)
        {
            var sbw = new FreeShippingByOrderTotalsRecord()
            {
                CountryId = model.CountryId,
                StateProvinceId = model.StateProvinceId,
                Zip = model.Zip == "*" ? null : model.Zip,
                ShippingMethodId = model.ShippingMethodId,
                OrderOver = model.OrderOver,
            };
            _freeShippingByOrderTotalService.InsertFreeShippingRecord(sbw);

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;
            return View("CLF.Plugin.Shipping.FreeShipping.Views.FreeShipping.AddPopup", model);
        }

        //edit
        public ActionResult EditPopup(int id)
        {
            var sbw = _freeShippingByOrderTotalService.GetById(id);
            if (sbw == null)
                //No record found with the specified id
                return RedirectToAction("Configure");

            var model = new ShippingFreeOrdersOverModel()
            {
                Id = sbw.Id,
                CountryId = sbw.CountryId,
                StateProvinceId = sbw.StateProvinceId,
                Zip = sbw.Zip,        
                OrderOver = sbw.OrderOver,
                PrimaryStoreCurrencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode
               
            };

            var shippingMethods = _shippingService.GetAllShippingMethods();
            if (shippingMethods.Count == 0)
                return Content("No shipping methods can be loaded");

            var selectedShippingMethod = _shippingService.GetShippingMethodById(sbw.ShippingMethodId);
            var selectedCountry = _countryService.GetCountryById(sbw.CountryId);
            var selectedState = _stateProvinceService.GetStateProvinceById(sbw.StateProvinceId);

            //shipping methods
            foreach (var sm in shippingMethods)
                model.AvailableShippingMethods.Add(new SelectListItem() { Text = sm.Name, Value = sm.Id.ToString(), Selected = (selectedShippingMethod != null && sm.Id == selectedShippingMethod.Id) });

            //countries
            model.AvailableCountries.Add(new SelectListItem() { Text = "*", Value = "0" });
            var countries = _countryService.GetAllCountries(true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem() { Text = c.Name, Value = c.Id.ToString(), Selected = (selectedCountry != null && c.Id == selectedCountry.Id) });
            //states
            var states = selectedCountry != null ? _stateProvinceService.GetStateProvincesByCountryId(selectedCountry.Id, true).ToList() : new List<StateProvince>();
            model.AvailableStates.Add(new SelectListItem() { Text = "*", Value = "0" });
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem() { Text = s.Name, Value = s.Id.ToString(), Selected = (selectedState != null && s.Id == selectedState.Id) });
            
            return View("CLF.Plugin.Shipping.FreeShipping.Views.FreeShipping.EditPopup", model);
        }
        [HttpPost]
        public ActionResult EditPopup(string btnId, string formId, ShippingFreeOrdersOverModel model)
        {
            var sbw = _freeShippingByOrderTotalService.GetById(model.Id);
            if (sbw == null)
                //No record found with the specified id
                return RedirectToAction("Configure");

            sbw.CountryId = model.CountryId;
            sbw.StateProvinceId = model.StateProvinceId;
            sbw.Zip = model.Zip == "*" ? null : model.Zip;
            sbw.ShippingMethodId = model.ShippingMethodId;
            sbw.OrderOver = model.OrderOver;
           
            _freeShippingByOrderTotalService.UpdateFreeShippingRecord(sbw);

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;
            return View("CLF.Plugin.Shipping.FreeShipping.Views.FreeShipping.EditPopup", model);
        }
    }
}
