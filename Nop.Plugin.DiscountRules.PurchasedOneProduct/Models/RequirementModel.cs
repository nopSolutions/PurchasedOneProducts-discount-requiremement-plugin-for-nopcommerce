using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.DiscountRules.PurchasedOneProduct.Models
{
    public class RequirementModel
    {
        public int DiscountId { get; set; }

        [NopResourceDisplayName("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products")]
        public string Products { get; set; }

        public int RequirementId { get; set; }
    }
}