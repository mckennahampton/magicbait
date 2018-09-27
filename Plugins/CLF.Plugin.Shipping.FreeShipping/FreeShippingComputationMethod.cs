using System;
using System.Web.Routing;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using CLF.Plugin.Shipping.FreeShipping.Data;
using CLF.Plugin.Shipping.FreeShipping.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace CLF.Plugin.Shipping.FreeShipping
{
    public class FreeShippingComputationMethod : BasePlugin, IShippingRateComputationMethod
    {
        #region Fields

        private readonly IShippingService _shippingService;
        private readonly IFreeShippingByOrderTotalService _freeShippingService;
        private readonly IPriceCalculationService _priceCalculationService;
        
        private readonly ShippingFreeOrdersOverObjectContext _objectContext;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor
        public FreeShippingComputationMethod(IShippingService shippingService,
            IFreeShippingByOrderTotalService freeshippingService,
            IPriceCalculationService priceCalculationService,             
            ShippingFreeOrdersOverObjectContext objectContext,
            ISettingService settingService)
        {
            this._shippingService = shippingService;
            this._freeShippingService = freeshippingService;
            this._priceCalculationService = priceCalculationService;            
            this._objectContext = objectContext;
            this._settingService = settingService;
        }
        #endregion

        #region Utilities

        private bool IsQualifiedForFreeShipping(decimal subTotal, int shippingMethodId, int countryId, int stateProvinceId, string zip)
        {
            try
            {
                var freeShippingConditionRecord = _freeShippingService.FindRecord(shippingMethodId,countryId, stateProvinceId, zip);

                if (freeShippingConditionRecord == null)
                {
                    return false;
                }
                else
                {
                    if (subTotal > freeShippingConditionRecord.OrderOver)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                //An exception ocurred computing free shipping, swallow the exception and continue with other methods
            }
                        
            return false;
        }
        
        #endregion

        #region Methods

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException("getShippingOptionRequest");

            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest.Items == null || getShippingOptionRequest.Items.Count == 0)
            {
                response.AddError("No shipment items");
                return response;
            }
            if (getShippingOptionRequest.ShippingAddress == null)
            {
                response.AddError("Shipping address is not set");
                return response;
            }
            
            int countryId = getShippingOptionRequest.ShippingAddress.CountryId.HasValue ? getShippingOptionRequest.ShippingAddress.CountryId.Value : 0;
            int stateProvinceId = getShippingOptionRequest.ShippingAddress.StateProvinceId.HasValue ? getShippingOptionRequest.ShippingAddress.StateProvinceId.Value : 0;
            string zip = getShippingOptionRequest.ShippingAddress.ZipPostalCode;
            decimal subTotal = decimal.Zero;

            foreach (var shoppingCartItem in getShippingOptionRequest.Items)
            {
                if (shoppingCartItem.IsFreeShipping || !shoppingCartItem.IsShipEnabled)
                    continue;
                subTotal += _priceCalculationService.GetSubTotal(shoppingCartItem, true);
            }
           
            var shippingMethods = _shippingService.GetAllShippingMethods(countryId);

            foreach (var shippingMethod in shippingMethods)
            {
                bool OfferFreeShipping = IsQualifiedForFreeShipping(subTotal, shippingMethod.Id, countryId, stateProvinceId, zip);
                
                if (OfferFreeShipping)
                {
                    var shippingOption = new ShippingOption();
                    shippingOption.Name = shippingMethod.GetLocalized(x => x.Name);
                    shippingOption.Description = shippingMethod.GetLocalized(x => x.Description);
                    shippingOption.Rate = decimal.Zero;
                    response.ShippingOptions.Add(shippingOption);
                }
            }      

            return response;
        }

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return null;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "FreeShippingOrdersOver";
            routeValues = new RouteValueDictionary() { { "Namespaces", "CLF.Plugin.Shipping.FreeShipping.Controllers" }, { "area", null } };
        }
        
        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {         

            //database objects
            _objectContext.Install();

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.Country", "Country");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.Country.Hint", "If an asterisk is selected, then this shipping rate will apply to all customers, regardless of the country.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.StateProvince", "State / province");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.StateProvince.Hint", "If an asterisk is selected, then this shipping rate will apply to all customers from the given country, regardless of the state.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.Zip", "Zip");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.Zip.Hint", "Zip / postal code. If zip is empty, then this shipping rate will apply to all customers from the given country or state, regardless of the zip code.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.ShippingMethod", "Shipping method");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.ShippingMethod.Hint", "The shipping method.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.OrderOver", "Orders Over");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.OrderOver.Hint", "Orders Over.");            
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.AddRecord", "Add record");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.AdminDescriptionTitle", "Countries with free shipping");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Title", "Free Shipping for order over X");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FreeShipping.Description", "<ul><li> First Create a Shipping Method from Configuration -> Shipping -> Shipping Methods, you could call it 'Free Shipping'. The only purpose of this is to be able to manipulate the text that will be displayed to users during checkout.</li><li>Then in this page you Add a New Record and select the conditions for free shipping and select the previous shipping method you had create on Step 1.</li></ul>");
            
            
            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
           
            //database objects
            _objectContext.Uninstall();

            //locales
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.Country");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.Country.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.StateProvince");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.StateProvince.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.Zip");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.Zip.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.ShippingMethod");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.ShippingMethod.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.OrderOver");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.OrderOver.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Fields.DataHtml");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.AddRecord");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.AdminDescriptionTitle");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Title");
            this.DeletePluginLocaleResource("Plugins.Shipping.FreeShipping.Description");
            
            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType
        {
            get
            {
                return ShippingRateComputationMethodType.Offline;
            }
        }


        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker
        {
            get
            {
                //uncomment a line below to return a general shipment tracker (finds an appropriate tracker by tracking number)
                //return new GeneralShipmentTracker(EngineContext.Current.Resolve<ITypeFinder>());
                return null; 
            }
        }

        #endregion
    }
}
