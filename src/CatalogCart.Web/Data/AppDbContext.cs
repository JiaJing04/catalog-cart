using CatalogCart.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogCart.Web.Data;

/// <summary>
/// The local SQLite database (App_Data/catalogcart.db). It holds:
///  - Products and ProductVariations: the offline copy of the last catalog download.
///  - CartItems: the cart, so it survives refreshes and restarts.
///  - SyncInfo: when the catalog was last downloaded successfully.
/// Relationships and keys follow EF Core conventions (Id, ProductId + navigation).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariation> ProductVariations => Set<ProductVariation>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<SyncInfo> SyncInfo => Set<SyncInfo>();
}

/// <summary>Single-row table recording the last successful catalog download.</summary>
public class SyncInfo
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public DateTime LastSyncedUtc { get; set; }
}
