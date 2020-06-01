using System.Text.RegularExpressions;
using FluentValidation;
using Nop.Plugin.DiscountRules.PurchasedOneProduct.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.DiscountRules.PurchasedOneProduct.Validators
{
    /// <summary>
    /// Represents an <see cref="RequirementModel"/> validator.
    /// </summary>
    public class RequirementModelValidator : BaseNopValidator<RequirementModel>
    {
        public RequirementModelValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.DiscountId)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.DiscountId.Required"));
            RuleFor(model => model.ProductIds)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.ProductIds.Required"));
            RuleFor(model => model.ProductIds)
                .Must(value => !Regex.IsMatch(value, @"(?!\d+)(?:[^ ,])"))
                .WithMessage(localizationService.GetResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.ProductIds.InvalidFormat"))
                .When(model => !string.IsNullOrWhiteSpace(model.ProductIds));
        }
    }
}
