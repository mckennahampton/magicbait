using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace CLF.Plugin.Shipping.FreeShipping
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.Shipping.FreeShipping.Configure",
                 "Plugins/FreeShippingOrdersOver/Configure",
                 new { controller = "FreeShippingOrdersOver", action = "Configure" },
                 new[] { "CLF.Plugin.Shipping.FreeShipping.Controllers" }
            );
            //routes.MapRoute("Plugin.Shipping.FreeShipping.SaveGeneralSettings",
            //     "Plugins/FreeShippingOrdersOver/SaveGeneralSettings",
            //     new { controller = "FreeShippingOrdersOver", action = "SaveGeneralSettings", },
            //     new[] { "CLF.Plugin.Shipping.FreeShipping.Controllers" }
            //);
            routes.MapRoute("Plugin.Shipping.FreeShipping.AddPopup",
                 "Plugins/FreeShippingOrdersOver/AddPopup",
                 new { controller = "FreeShippingOrdersOver", action = "AddPopup" },
                 new[] { "CLF.Plugin.Shipping.FreeShipping.Controllers" }
            );
            routes.MapRoute("Plugin.Shipping.FreeShipping.EditPopup",
                 "Plugins/FreeShippingOrdersOver/EditPopup",
                 new { controller = "FreeShippingOrdersOver", action = "EditPopup" },
                 new[] { "CLF.Plugin.Shipping.FreeShipping.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
