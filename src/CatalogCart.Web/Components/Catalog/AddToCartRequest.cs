using CatalogCart.Web.Models;

namespace CatalogCart.Web.Components.Catalog;

/// <summary>What a product row asks the page to add: the product, the chosen UOM and the quantity.</summary>
public sealed record AddToCartRequest(Product Product, ProductVariation? Variation, int Quantity);
