using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace CLF.Plugin.Shipping.FreeShipping.Models
{
    public class ShippingFreeOrdersOverModel : BaseNopEntityModel
    {
        public ShippingFreeOrdersOverModel()
        {
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
            AvailableShippingMethods = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Shipping.FreeShipping.Fields.Country")]
        public int CountryId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FreeShipping.Fields.Country")]
        public string CountryName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FreeShipping.Fields.StateProvince")]
        public int StateProvinceId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FreeShipping.Fields.StateProvince")]
        public string StateProvinceName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FreeShipping.Fields.Zip")]
        public string Zip { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FreeShipping.Fields.ShippingMethod")]
        public int ShippingMethodId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FreeShipping.Fields.ShippingMethod")]
        public string ShippingMethodName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FreeShipping.Fields.OrderOver")]
        public decimal OrderOver { get; set; }             
        
       
        public string PrimaryStoreCurrencyCode { get; set; }  


        public IList<SelectListItem> AvailableCountries { get; set; }
        public IList<SelectListItem> AvailableStates { get; set; }
        public IList<SelectListItem> AvailableShippingMethods { get; set; }
        
    }
}