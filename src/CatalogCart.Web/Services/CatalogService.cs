using System.Text.Json;
using CatalogCart.Web.Api;
using CatalogCart.Web.Data;
using CatalogCart.Web.Models;

namespace CatalogCart.Web.Services;

/// <summary>
/// Decides where the catalog comes from: the API when it can be reached, otherwise the
/// SQLite copy saved by the last successful download.
/// </summary>
public sealed class CatalogService(
    IProductApiClient api,
    IProductCache cache,
    TimeProvider clock,
    ILogger<CatalogService> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CatalogSnapshot? _current;

    /// <summary>
    /// Returns the catalog, downloading it the first time. Later calls reuse the copy in
    /// memory, so moving between pages doesn't download every page again.
    /// </summary>
    public async Task<CatalogSnapshot> GetCatalogAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && _current is not null)
        {
            return _current;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _current is not null)
            {
                return _current;
            }

            _current = await LoadAsync(cancellationToken);
            return _current;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CatalogSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Product> downloaded;
        try
        {
            downloaded = await api.GetAllProductsAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConnectivityProblem(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Products API unavailable; falling back to the local cache.");
            return await LoadFromCacheAsync(ex, cancellationToken);
        }

        var syncedAt = clock.GetUtcNow().UtcDateTime;
        try
        {
            await cache.ReplaceAllAsync(downloaded, syncedAt, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Saving the offline copy failed, but the live data is still good to show.
            logger.LogError(ex, "Could not save the catalog to the local cache.");
        }

        return new CatalogSnapshot(OnlyVariable(downloaded), CatalogSource.Live, syncedAt, Notice: null);
    }

    private async Task<CatalogSnapshot> LoadFromCacheAsync(Exception reason, CancellationToken cancellationToken)
    {
        var (products, lastSynced) = await cache.LoadAsync(cancellationToken);

        var notice = reason is ApiNotConfiguredException
            ? "API not configured – showing saved products. Fill in BoostOrderApi settings."
            : "You're offline – showing saved products.";

        return new CatalogSnapshot(OnlyVariable(products), CatalogSource.OfflineCache, lastSynced, notice);
    }

    /// <summary>Catalog only show "variable" products.</summary>
    private static IReadOnlyList<Product> OnlyVariable(IEnumerable<Product> products) =>
        products.Where(product => product.IsVariable).ToList();

    /// <summary>
    /// Failures to use the offline copy: no network, a timeout, an HTTP error
    /// status, an unreadable payload or missing config.
    /// </summary>
    private static bool IsConnectivityProblem(Exception ex, CancellationToken cancellationToken) => ex switch
    {
        HttpRequestException => true,
        TaskCanceledException when !cancellationToken.IsCancellationRequested => true, // HttpClient timeout
        JsonException => true,
        NotSupportedException => true, // Response wasn't JSON.
        ApiNotConfiguredException => true,
        _ => false,
    };
}
