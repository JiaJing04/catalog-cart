using System.Text.Json.Serialization;
using CatalogCart.Web.Api.Json;

namespace CatalogCart.Web.Api.Dtos;

// Mirrors BoostOrder's API payload, only the fields the app uses.
// ProductMapper turns these into app models, so payload changes stay in the Api folder.

public sealed class ProductDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("price")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal? Price { get; set; }

    [JsonPropertyName("regular_price")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal? RegularPrice { get; set; }

    [JsonPropertyName("stock_quantity")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int? StockQuantity { get; set; }

    [JsonPropertyName("in_stock")]
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool? InStock { get; set; }

    /// <summary>Only used for a product with no variations.</summary>
    [JsonPropertyName("backorders_allowed")]
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool? BackordersAllowed { get; set; }

    [JsonPropertyName("images")]
    public List<ImageDto>? Images { get; set; }

    [JsonPropertyName("variations")]
    public List<VariationDto>? Variations { get; set; }
}

public sealed class VariationDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("price")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal? Price { get; set; }

    [JsonPropertyName("regular_price")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal? RegularPrice { get; set; }

    [JsonPropertyName("stock_quantity")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int? StockQuantity { get; set; }

    [JsonPropertyName("in_stock")]
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool? InStock { get; set; }

    [JsonPropertyName("attributes")]
    public List<AttributeDto>? Attributes { get; set; }

    /// <summary>
    /// Whether this UOM can be ordered beyond its stock.
    /// </summary>
    [JsonPropertyName("backorders_allowed")]
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool? BackordersAllowed { get; set; }

    /// <summary>Stock per branch. BoostOrder leaves stock_quantity at 0, so ProductMapper sums these rows instead.</summary>
    [JsonPropertyName("inventory")]
    public List<InventoryDto>? Inventory { get; set; }

    /// <summary>BoostOrder's UOM label for this variation, e.g. "UNIT" or "CARTON".</summary>
    [JsonPropertyName("uom")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Uom { get; set; }
}

public sealed class AttributeDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("option")]
    public string? Option { get; set; }
}

public sealed class InventoryDto
{
    [JsonPropertyName("branch_id")]
    public int? BranchId { get; set; }

    /// <summary>Can be a fraction (e.g. 1.333333 units) or negative (oversold).</summary>
    [JsonPropertyName("stock_quantity")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal? StockQuantity { get; set; }
}

public sealed class ImageDto
{
    [JsonPropertyName("src")]
    public string? Src { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }
}
