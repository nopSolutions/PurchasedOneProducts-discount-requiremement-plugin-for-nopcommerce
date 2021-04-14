using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.DiscountRules.PurchasedOneProduct.Models
{
    public record RequirementModel
    {
        public int DiscountId { get; set; }

        [NopResourceDisplayName("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products")]
        public string ProductIds { get; set; }

        public int RequirementId { get; set; }
    }
}