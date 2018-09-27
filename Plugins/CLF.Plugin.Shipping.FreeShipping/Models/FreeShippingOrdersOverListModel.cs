using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace CLF.Plugin.Shipping.FreeShipping.Models
{
    public class FreeShippingOrdersOverListModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Shipping.FreeShippingOrdesOver.Fields.OrdersOver")]
        public decimal OrdersOver { get; set; }
        
    }
}