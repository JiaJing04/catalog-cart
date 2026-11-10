using CatalogCart.Web.Api.Dtos;
using CatalogCart.Web.Models;

namespace CatalogCart.Web.Api;

/// <summary>Turns API data shapes into the app's own models.</summary>
public static class ProductMapper
{
    public static Product ToProduct(ProductDto dto)
    {
        var variations = (dto.Variations ?? [])
            .Select((variation, index) => ToVariation(variation, dto.Id, index))
            .ToList();

        return new Product
        {
            Id = dto.Id,
            Name = dto.Name?.Trim() ?? string.Empty,
            Sku = dto.Sku?.Trim() ?? string.Empty,
            Type = dto.Type?.Trim() ?? string.Empty,
            Price = dto.Price ?? dto.RegularPrice ?? variations.FirstOrDefault()?.Price ?? 0m,
            StockQuantity = KnownQuantity(dto.StockQuantity),
            InStock = dto.InStock ?? (dto.StockQuantity is null or > 0),
            BackordersAllowed = dto.BackordersAllowed ?? false,
            ImageUrl = dto.Images?
                .OrderBy(image => image.Position)
                .Select(image => image.Src)
                .FirstOrDefault(src => !string.IsNullOrWhiteSpace(src)),
            Variations = variations,
        };
    }

    private static ProductVariation ToVariation(VariationDto dto, int productId, int position) => new()
    {
        Id = dto.Id,
        ProductId = productId,
        Position = position,
        UomName = ResolveUomName(dto, position),
        Sku = dto.Sku?.Trim() ?? string.Empty,
        Price = dto.Price ?? dto.RegularPrice ?? 0m,
        StockQuantity = ResolveStock(dto),
        InStock = dto.InStock ?? (dto.StockQuantity is null or > 0),
        BackordersAllowed = dto.BackordersAllowed ?? false,
    };

    /// <summary>
    /// BoostOrder's stock_quantity is 0 on variations; real stock is per branch in "inventory".
    /// Sums the branches (negatives included), clamped at 0, rounded down. With no branch rows, returns null
    /// (unknown) unless stock_quantity is above 0.
    /// </summary>
    private static int? ResolveStock(VariationDto dto)
    {
        var branches = dto.Inventory?
            .Where(row => row.StockQuantity is not null)
            .ToList();

        if (branches is { Count: > 0 })
        {
            var total = Math.Max(branches.Sum(row => row.StockQuantity!.Value), 0m);
            return (int)Math.Floor(Math.Min(total, int.MaxValue));
        }

        return KnownQuantity(dto.StockQuantity);
    }

    /// <summary>
    /// Fallback for the top-level stock_quantity, used only when there are no inventory rows.
    /// BoostOrder leaves this field at 0 (real stock is per branch in inventory),
    /// so treat 0 or less as "unknown/not tracked" rather than "sold out".
    /// </summary>
    private static int? KnownQuantity(int? quantity) => quantity is > 0 ? quantity : null;

    /// <summary>
    /// Picks the label for the UOM dropdown: a dedicated UOM field if there is one.
    /// </summary>
    private static string ResolveUomName(VariationDto dto, int position)
    {
        if (!string.IsNullOrWhiteSpace(dto.Uom))
        {
            return dto.Uom.Trim().ToUpperInvariant();
        }

        var option = dto.Attributes?
            .Select(attribute => attribute.Option)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        return option is not null
            ? option.Trim().ToUpperInvariant()
            : $"OPTION {position + 1}";
    }
}
