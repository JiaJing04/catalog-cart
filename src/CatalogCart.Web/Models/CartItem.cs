using System.ComponentModel.DataAnnotations.Schema;

namespace CatalogCart.Web.Models;

/// <summary>
/// One line in the cart: a product in a specific UOM. Product details are copied in when
/// the line is added, so the cart still renders correctly if the catalog cache changes.
/// </summary>
public class CartItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    /// <summary>Null only for a product without variations.</summary>
    public int? VariationId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// The UOM's SKU (e.g. "BoBread001-UNIT"). Saved for future orders; not shown.
    /// Lines are matched by ProductId + VariationId, not SKU.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>The product SKU (e.g. "BoBread001"), shown in the cart.</summary>
    public string ProductSku { get; set; } = string.Empty;

    public string UomName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public int? StockQuantity { get; set; }

    public bool InStock { get; set; }

    /// <summary>
    /// Copied from the UOM when added. False caps the quantity at StockQuantity (when known);
    /// true allows more, with an "Only N in stock" warning.
    /// </summary>
    public bool BackordersAllowed { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime AddedAtUtc { get; set; }

    /// <summary>Calculated each time, so it always matches price × quantity.</summary>
    [NotMapped]
    public decimal LineTotal => UnitPrice * Quantity;
}
