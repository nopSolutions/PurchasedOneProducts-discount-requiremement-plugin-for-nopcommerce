﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Discounts;
using Nop.Plugin.DiscountRules.PurchasedOneProduct.Models;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.DiscountRules.PurchasedOneProduct.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class DiscountRulesPurchasedOneProductController : BasePluginController
    {
        #region Fields

        private readonly IDiscountService _discountService;
        private readonly IPermissionService _permissionService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public DiscountRulesPurchasedOneProductController(IDiscountService discountService,
            IPermissionService permissionService,
            IProductModelFactory productModelFactory,
            IProductService productService,
            ISettingService settingService)
        {
            _discountService = discountService;
            _permissionService = permissionService;
            _productModelFactory = productModelFactory;
            _productService = productService;
            _settingService = settingService;
        }

        #endregion

        #region Methods

        public IActionResult Configure(int discountId, int? discountRequirementId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);

            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            //check whether the discount requirement exists
            if (discountRequirementId.HasValue && _discountService.GetDiscountRequirementById(discountRequirementId.Value) is null)
                return Content("Failed to load requirement.");

            var restrictedProductVariantIds = _settingService.GetSettingByKey<string>(string.Format(DiscountRequirementDefaults.SETTINGS_KEY, discountRequirementId ?? 0));

            var model = new RequirementModel
            {
                RequirementId = discountRequirementId ?? 0,
                DiscountId = discountId,
                ProductIds = restrictedProductVariantIds
            };

            //add a prefix
            ViewData.TemplateInfo.HtmlFieldPrefix = string.Format(DiscountRequirementDefaults.HTML_FIELD_PREFIX, discountRequirementId ?? 0);

            return View("~/Plugins/DiscountRules.PurchasedOneProduct/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(RequirementModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            if (ModelState.IsValid)
            {
                //load the discount
                var discount = _discountService.GetDiscountById(model.DiscountId);
                if (discount == null)
                    return NotFound(new { Errors = new[] { "Discount could not be loaded" } });

                //get the discount requirement
                var discountRequirement = _discountService.GetDiscountRequirementById(model.RequirementId);

                //the discount requirement does not exist, so create a new one
                if (discountRequirement == null)
                {
                    discountRequirement = new DiscountRequirement
                    {
                        DiscountId = discount.Id,
                        DiscountRequirementRuleSystemName = DiscountRequirementDefaults.SYSTEM_NAME
                    };

                    _discountService.InsertDiscountRequirement(discountRequirement);
                }

                //save restricted products
                _settingService.SetSetting(string.Format(DiscountRequirementDefaults.SETTINGS_KEY, discountRequirement.Id), model.ProductIds);

                return Ok(new { NewRequirementId = discountRequirement.Id });
            }

            return BadRequest(new { Errors = GetErrorsFromModelState() });
        }

        public IActionResult ProductAddPopup(string btnId, string productIdsInput)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            ViewBag.productIdsInput = productIdsInput;
            ViewBag.btnId = btnId;

            //prepare model
            var model = _productModelFactory.PrepareProductSearchModel(new ProductSearchModel());

            return View("~/Plugins/DiscountRules.PurchasedOneProduct/Views/ProductAddPopup.cshtml", model);
        }


        [HttpPost]
        public IActionResult LoadProductFriendlyNames(string productIds)
        {
            var result = "";

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return Json(new { Text = result });

            if (!string.IsNullOrWhiteSpace(productIds))
            {
                var ids = new List<int>();
                var idsArray = productIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                foreach (var str1 in idsArray)
                {
                    if (int.TryParse(str1, out var tmp1))
                        ids.Add(tmp1);
                }

                var products = _productService.GetProductsByIds(ids.ToArray());
                result = string.Join(", ", products.Select(p => p.Name));
            }

            return Json(new { Text = result });
        }

        #endregion

        #region Utilities

        private IEnumerable<string> GetErrorsFromModelState()
        {
            return ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
        }

        #endregion
    }
}