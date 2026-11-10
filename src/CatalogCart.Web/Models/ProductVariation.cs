using Microsoft.EntityFrameworkCore;

namespace CatalogCart.Web.Models;

/// <summary>One unit of measurement (UOM) of a product, e.g. UNIT, BOX or CARTON.</summary>
/// <remarks>
/// Keyed by (ProductId, Position) rather than the API id, so a payload with missing or
/// repeated variation ids can still be cached instead of failing the whole save.
/// </remarks>
[PrimaryKey(nameof(ProductId), nameof(Position))]
public class ProductVariation
{
    /// <summary>The API's variation id.</summary>
    public int Id { get; set; }

    public int ProductId { get; set; }

    /// <summary>Keeps the order the API returned, so the dropdown order is stable offline.</summary>
    public int Position { get; set; }

    /// <summary>Label shown in the UOM dropdown and in the cart ("2 UNIT").</summary>
    public string UomName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int? StockQuantity { get; set; }

    public bool InStock { get; set; }

    /// <summary>False: the cart can't hold more than the stock. True: it can, with a warning.</summary>
    public bool BackordersAllowed { get; set; }
}
