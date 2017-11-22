using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.DiscountRules.PurchasedOneProduct
{
    public partial class RouteProvider : IRouteProvider
    {
        #region Methods

        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            routeBuilder.MapRoute("Plugin.DiscountRules.PurchasedOneProduct.Configure",
                 "Plugins/DiscountRulesPurchasedOneProduct/Configure",
                 new { controller = "DiscountRulesPurchasedOneProduct", action = "Configure" });

            routeBuilder.MapRoute("Plugin.DiscountRules.PurchasedOneProduct.ProductAddPopup",
                 "Plugins/DiscountRulesPurchasedOneProduct/ProductAddPopup",
                 new { controller = "DiscountRulesPurchasedOneProduct", action = "ProductAddPopup" });

            routeBuilder.MapRoute("Plugin.DiscountRules.PurchasedOneProduct.ProductAddPopupList",
                 "Plugins/DiscountRulesPurchasedOneProduct/ProductAddPopupList",
                 new { controller = "DiscountRulesPurchasedOneProduct", action = "ProductAddPopupList" });

            routeBuilder.MapRoute("Plugin.DiscountRules.PurchasedOneProduct.LoadProductFriendlyNames",
                 "Plugins/DiscountRulesPurchasedOneProduct/LoadProductFriendlyNames",
                 new { controller = "DiscountRulesPurchasedOneProduct", action = "LoadProductFriendlyNames" });
        }

        #endregion

        #region Properties

        public int Priority
        {
            get
            {
                return 0;
            }
        }

        #endregion
    }
}
