using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.DiscountRules.PurchasedOneProduct;

public class PurchasedOneProductDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
{
    #region Fields

    private const char IDS_SEPARATOR = ',';
    private readonly IDiscountService _discountService;
    private readonly ILocalizationService _localizationService;
    private readonly INopUrlHelper _nopUrlHelper;
    private readonly IOrderService _orderService;
    private readonly IReturnRequestService _returnRequestService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public PurchasedOneProductDiscountRequirementRule(
        IDiscountService discountService,
        ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IOrderService orderService,
        IReturnRequestService returnRequestService,
        ISettingService settingService,
        IWebHelper webHelper)
    {
        _discountService = discountService;
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _orderService = orderService;
        _returnRequestService = returnRequestService;
        _settingService = settingService;
        _webHelper = webHelper;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Check discount requirement
    /// </summary>
    /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc.)</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    public async Task<DiscountRequirementValidationResult> CheckRequirementAsync(DiscountRequirementValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        //invalid by default
        var result = new DiscountRequirementValidationResult();

        var restrictedProductVariantIdsStr = await _settingService.GetSettingByKeyAsync<string>(string.Format(DiscountRequirementDefaults.SETTINGS_KEY, request.DiscountRequirementId));

        if (string.IsNullOrWhiteSpace(restrictedProductVariantIdsStr))
            return result;

        if (request.Customer == null)
            return result;

        List<int> restrictedProductIds;

        try
        {
            restrictedProductIds = restrictedProductVariantIdsStr
                .Split(IDS_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToInt32(x))
                .ToList();
        }
        catch
        {
            //error parsing
            return result;
        }

        if (restrictedProductIds.Count == 0)
            return result;

        var customerId = request.Customer.Id;

        //get available orders
        var availableOrders = await _orderService.SearchOrdersAsync(
            customerId: customerId,
            osIds: [(int)OrderStatus.Complete]);

        //get all available purchased product ids
        var purchasedProductIds = await availableOrders
            .SelectManyAwait(async order => await _orderService.GetOrderItemsAsync(order.Id))
            .Where(async (orderItem, _) =>
            {
                //exclude products from return requests
                var returnRequests =
                    (await _returnRequestService.SearchReturnRequestsAsync(customerId: customerId,
                        orderItemId: orderItem.Id))
                    .Where(returnRequest => returnRequest.ReturnRequestStatus != ReturnRequestStatus.Cancelled &&
                                            returnRequest.ReturnRequestStatus != ReturnRequestStatus.RequestRejected &&
                                            returnRequest.ReturnRequestStatus != ReturnRequestStatus.ItemsRepaired);
                var returnedQuantity = 0;
                foreach (var returnRequest in returnRequests)
                    returnedQuantity += returnRequest.Quantity;

                return returnedQuantity < orderItem.Quantity;

            })
            .Select(orderItem => orderItem.ProductId)
            .Distinct()
            .ToListAsync();

        //check if any purchased products are match the restricted products
        result.IsValid = restrictedProductIds
            .Any(productId => purchasedProductIds.Any(purchasedProductId => purchasedProductId == productId));

        return result;
    }

    /// <summary>
    /// Get URL for rule configuration
    /// </summary>
    /// <param name="discountId">Discount identifier</param>
    /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
    /// <returns>URL</returns>
    public string GetConfigurationUrl(int discountId, int? discountRequirementId)
    {
        return _nopUrlHelper.RouteUrl(DiscountRequirementDefaults.ConfigurationRouteName,
            new { discountId, discountRequirementId }, _webHelper.GetCurrentRequestProtocol());
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.DiscountRules.PurchasedOneProduct.Fields.Products"] = "Restricted products",
            ["Plugins.DiscountRules.PurchasedOneProduct.Fields.Products.Hint"] = "The comma-separated list of product identifiers (e.g. 77, 123, 156). You can find a product variant ID on its details page.",
            ["Plugins.DiscountRules.PurchasedOneProduct.Fields.Products.AddNew"] = "Add product",
            ["Plugins.DiscountRules.PurchasedOneProduct.Fields.Products.Choose"] = "Choose",
            ["Plugins.DiscountRules.PurchasedOneProduct.Fields.ProductIds.Required"] = "Products are required",
            ["Plugins.DiscountRules.PurchasedOneProduct.Fields.DiscountId.Required"] = "Discount is required",
            ["Plugins.DiscountRules.PurchasedOneProduct.Fields.ProductIds.InvalidFormat"] = "Invalid format of the products selection. Format should be comma-separated list of product identifiers (e.g. 77, 123, 156). You can find a product ID on its details page."
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        //discount requirements
        var discountRequirements = (await _discountService.GetAllDiscountRequirementsAsync())
            .Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == DiscountRequirementDefaults.SYSTEM_NAME);

        foreach (var discountRequirement in discountRequirements)
            await _discountService.DeleteDiscountRequirementAsync(discountRequirement, false);

        //locales
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.DiscountRules.PurchasedOneProduct");

        await base.UninstallAsync();
    }

    #endregion
}