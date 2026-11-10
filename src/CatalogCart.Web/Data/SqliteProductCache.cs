using CatalogCart.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogCart.Web.Data;

/// <summary>The offline copy of the catalog.</summary>
public interface IProductCache
{
    Task<(IReadOnlyList<Product> Products, DateTime? LastSyncedUtc)> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the whole cache with a fresh download.</summary>
    Task ReplaceAllAsync(IReadOnlyList<Product> products, DateTime syncedAtUtc, CancellationToken cancellationToken = default);
}

public sealed class SqliteProductCache(IDbContextFactory<AppDbContext> dbFactory) : IProductCache
{
    public async Task<(IReadOnlyList<Product> Products, DateTime? LastSyncedUtc)> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var products = await db.Products
            .AsNoTracking()
            .Include(product => product.Variations)
            .OrderBy(product => product.SortOrder)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.Variations = product.Variations.OrderBy(variation => variation.Position).ToList();
        }

        var syncInfo = await db.SyncInfo.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return (products, syncInfo?.LastSyncedUtc);
    }

    /// <remarks>
    /// Replace rather than merge: a product removed from the API should disappear from the
    /// offline catalog too. It runs in one transaction, so a failure halfway through
    /// leaves the previous cache intact.
    /// </remarks>
    public async Task ReplaceAllAsync(IReadOnlyList<Product> products, DateTime syncedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.ProductVariations.ExecuteDeleteAsync(cancellationToken);
        await db.Products.ExecuteDeleteAsync(cancellationToken);
        await db.SyncInfo.ExecuteDeleteAsync(cancellationToken);

        // Copies, so EF never tracks the objects the UI is already showing.
        db.Products.AddRange(products.Select(Clone));
        db.SyncInfo.Add(new SyncInfo { LastSyncedUtc = syncedAtUtc });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static Product Clone(Product source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Sku = source.Sku,
        Type = source.Type,
        Price = source.Price,
        StockQuantity = source.StockQuantity,
        InStock = source.InStock,
        BackordersAllowed = source.BackordersAllowed,
        ImageUrl = source.ImageUrl,
        SortOrder = source.SortOrder,
        Variations = source.Variations.Select(variation => new ProductVariation
        {
            Id = variation.Id,
            ProductId = source.Id,
            Position = variation.Position,
            UomName = variation.UomName,
            Sku = variation.Sku,
            Price = variation.Price,
            StockQuantity = variation.StockQuantity,
            InStock = variation.InStock,
            BackordersAllowed = variation.BackordersAllowed,
        }).ToList(),
    };
}
