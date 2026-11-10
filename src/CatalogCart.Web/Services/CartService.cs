using CatalogCart.Web.Data;
using CatalogCart.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogCart.Web.Services;

/// <summary>
/// The cart. It keeps an in-memory list for instant UI updates and writes every change
/// through to SQLite, so the cart survives refreshes and restarts.
/// </summary>
public sealed class CartService(IDbContextFactory<AppDbContext> dbFactory, TimeProvider clock)
{
    public const int MaxQuantity = 999;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<CartItem>? _items;

    /// <summary>Raised after any change to the cart.</summary>
    public event Action? Changed;

    public IReadOnlyList<CartItem> Items => _items ?? [];

    /// <summary>Number of lines (one per product + UOM).</summary>
    public int LineCount => Items.Count;

    public decimal Total => Items.Sum(item => item.LineTotal);

    /// <summary>Loads the saved cart from SQLite the first time it's needed.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_items is not null) return;

        await _gate.WaitAsync();
        try
        {
            if (_items is not null) return;

            await using var db = await dbFactory.CreateDbContextAsync();
            _items = await db.CartItems.AsNoTracking().OrderBy(item => item.AddedAtUtc).ToListAsync();
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// The most a line may hold.
    /// </summary>
    public static int LimitFor(int? stockQuantity, bool backordersAllowed) =>
        !backordersAllowed && stockQuantity is int stock
            ? Math.Clamp(stock, 0, MaxQuantity)
            : MaxQuantity;

    /// <summary>Quantity of this product already in the cart.</summary>
    public int QuantityInCart(int productId, int? variationId) =>
        Items.Where(item => item.ProductId == productId && item.VariationId == variationId)
             .Sum(item => item.Quantity);

    /// <summary>
    /// Adds a product in the chosen UOM. Adding the same product + UOM again increases
    /// that line's quantity instead of creating a duplicate line. If the UOM doesn't allow
    /// backorders, the line is capped at the stock. Returns how many were actually added.
    /// </summary>
    public async Task<int> AddAsync(Product product, ProductVariation? variation, int quantity)
    {
        var added = 0;
        await MutateAsync(async (db, items) =>
        {
            quantity = Math.Clamp(quantity, 1, MaxQuantity);
            var uomName = variation?.UomName ?? "UNIT";
            var stock = variation?.StockQuantity ?? product.StockQuantity;
            var backorders = variation?.BackordersAllowed ?? product.BackordersAllowed;
            var limit = LimitFor(stock, backorders);

            var existing = items.FirstOrDefault(item =>
                item.ProductId == product.Id &&
                item.VariationId == variation?.Id &&
                item.UomName == uomName);

            added = Math.Clamp(limit - (existing?.Quantity ?? 0), 0, quantity);
            if (added == 0) return;

            if (existing is not null)
            {
                existing.Quantity += added;
                // Keep the line's stock info as fresh as the catalog it was just added from.
                existing.StockQuantity = stock;
                existing.BackordersAllowed = backorders;
                db.CartItems.Update(existing);
            }
            else
            {
                var line = new CartItem
                {
                    ProductId = product.Id,
                    VariationId = variation?.Id,
                    ProductName = product.Name,
                    Sku = string.IsNullOrEmpty(variation?.Sku) ? product.Sku : variation.Sku,
                    ProductSku = string.IsNullOrEmpty(product.Sku) ? variation?.Sku ?? string.Empty : product.Sku,
                    UomName = uomName,
                    UnitPrice = variation?.Price ?? product.Price,
                    Quantity = added,
                    StockQuantity = stock,
                    InStock = variation?.InStock ?? product.InStock,
                    BackordersAllowed = backorders,
                    ImageUrl = product.ImageUrl,
                    AddedAtUtc = clock.GetUtcNow().UtcDateTime,
                };
                db.CartItems.Add(line);
                items.Add(line);
            }

            await db.SaveChangesAsync();
        });
        return added;
    }

    public Task UpdateQuantityAsync(int lineId, int quantity) =>
        MutateAsync(async (db, items) =>
        {
            var line = items.FirstOrDefault(item => item.Id == lineId);
            if (line is null) return;

            // Same cap as adding; never below 1.
            var limit = Math.Max(1, LimitFor(line.StockQuantity, line.BackordersAllowed));
            line.Quantity = Math.Clamp(quantity, 1, limit);
            db.CartItems.Update(line);
            await db.SaveChangesAsync();
        });

    public Task RemoveAsync(int lineId) =>
        MutateAsync(async (db, items) =>
        {
            var line = items.FirstOrDefault(item => item.Id == lineId);
            if (line is null) return;

            items.Remove(line);
            db.CartItems.Remove(line);
            await db.SaveChangesAsync();
        });

    public Task ClearAsync() =>
        MutateAsync(async (db, items) =>
        {
            items.Clear();
            await db.CartItems.ExecuteDeleteAsync();
        });

    /// <summary>One change at a time: lock, apply to memory and SQLite, then notify.</summary>
    private async Task MutateAsync(Func<AppDbContext, List<CartItem>, Task> change)
    {
        await EnsureLoadedAsync();

        await _gate.WaitAsync();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            await change(db, _items!);

            // Detach so the next change can attach these objects to a new context.
            db.ChangeTracker.Clear();
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke();
    }
}
