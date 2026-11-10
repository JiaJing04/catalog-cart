namespace CatalogCart.Web.Models;

public enum CatalogSource
{
    /// <summary>Fresh from the API.</summary>
    Live,

    /// <summary>The API could not be reached; served from the local SQLite cache.</summary>
    OfflineCache,
}

/// <summary>The catalog products, and whether they came live or from the offline copy.</summary>
public sealed record CatalogSnapshot(
    IReadOnlyList<Product> Products,
    CatalogSource Source,
    DateTime? LastSyncedUtc,
    string? Notice);
