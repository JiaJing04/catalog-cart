using System.Net.Http.Json;
using System.Text.Json;
using CatalogCart.Web.Api.Dtos;
using CatalogCart.Web.Models;
using CatalogCart.Web.Options;
using Microsoft.Extensions.Options;

namespace CatalogCart.Web.Api;

public interface IProductApiClient
{
    /// <summary>Downloads every product, following the API's pagination.</summary>
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Thrown when the placeholders in appsettings.json have not been replaced.</summary>
public sealed class ApiNotConfiguredException()
    : InvalidOperationException("The BoostOrder API URL and credentials have not been configured.");

public sealed class ProductApiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<BoostOrderApiOptions> options,
    ILogger<ProductApiClient> logger) : IProductApiClient
{
    public const string HttpClientName = "BoostOrder";

    private const string TotalHeader = "X-WC-Total";
    private const string TotalPagesHeader = "X-WC-TotalPages";

    // Same settings ReadFromJsonAsync uses by default (case-insensitive names).
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly BoostOrderApiOptions _options = options.Value;

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            throw new ApiNotConfiguredException();
        }

        var http = httpClientFactory.CreateClient(HttpClientName);
        var products = new List<Product>();
        var totalPages = 1;
        int? expectedTotal = null;

        // Fetched one page at a time on purpose: it goes easy on a
        // shared API and lets us stop early if a page comes back empty.
        for (var page = 1; page <= totalPages; page++)
        {
            using var response = await http.GetAsync(BuildPageUrl(page), cancellationToken);
            response.EnsureSuccessStatusCode();

            if (page == 1)
            {
                var reportedPages = ReadIntHeader(response, TotalPagesHeader) ?? 1;
                totalPages = Math.Clamp(reportedPages, 1, _options.MaxPages);
                expectedTotal = ReadIntHeader(response, TotalHeader);

                if (reportedPages > _options.MaxPages)
                {
                    logger.LogWarning(
                        "API reports {Reported} pages but MaxPages is {Max}; later pages will be skipped. " +
                        "Raise BoostOrderApi:MaxPages.",
                        reportedPages, _options.MaxPages);
                }
            }

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var items = ReadProducts(body);
            if (items.Count == 0)
            {
                break; // Nothing more to read, whatever the header said.
            }

            products.AddRange(items.Select(ProductMapper.ToProduct));
        }

        logger.LogInformation(
            "Downloaded {Count} products in {Pages} page(s); API reported {Expected} in total.",
            products.Count, totalPages, expectedTotal?.ToString() ?? "unknown");

        // Counted before any filtering (e.g. "variable" only), so it counts the same products as the API total.
        if (expectedTotal is int expected && products.Count < expected)
        {
            logger.LogWarning(
                "Downloaded {Count} products but API reported {Expected}; some may be missing.",
                products.Count, expected);
        }

        // A product can appear on two pages if the catalog changes mid-download.
        var distinct = products.DistinctBy(product => product.Id).ToList();
        for (var i = 0; i < distinct.Count; i++)
        {
            distinct[i].SortOrder = i;
        }
        return distinct;
    }

    private static List<ProductDto> ReadProducts(JsonElement body)
    {
        var list = body.ValueKind switch
        {
            JsonValueKind.Array => body,
            JsonValueKind.Object when body.TryGetProperty("products", out var products) &&
                                      products.ValueKind == JsonValueKind.Array => products,
            _ => throw new JsonException($"Expected a product list, got a JSON {body.ValueKind}."),
        };
        return list.Deserialize<List<ProductDto>>(JsonOptions) ?? [];
    }

    private string BuildPageUrl(int page)
    {
        var separator = _options.ProductsUrl.Contains('?') ? '&' : '?';
        return $"{_options.ProductsUrl}{separator}page={page}";
    }

    private static int? ReadIntHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) &&
        int.TryParse(values.FirstOrDefault(), out var number)
            ? number
            : null;
}
