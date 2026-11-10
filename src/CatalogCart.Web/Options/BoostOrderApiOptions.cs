namespace CatalogCart.Web.Options;

/// <summary>
/// Settings for the BoostOrder products endpoint. loaded from the "BoostOrderApi" section.
/// Keep real credentials in user-secrets or environment variables, not in appsettings.json.
/// </summary>
public sealed class BoostOrderApiOptions
{
    public const string SectionName = "BoostOrderApi";

    /// <summary>Full URL of the products endpoint (without query string).</summary>
    public string ProductsUrl { get; set; } = string.Empty;

    /// <summary>Basic Auth username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Basic Auth password.</summary>
    public string Password { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>Safety cap so a bad X-WC-TotalPages header can never cause an endless loop.</summary>
    public int MaxPages { get; set; } = 200;

    /// <summary>False if the URL, username or password is missing or still a placeholder.</summary>
    public bool IsConfigured =>
        Uri.TryCreate(ProductsUrl, UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(Username) && !Username.StartsWith('<') &&
        !string.IsNullOrWhiteSpace(Password) && !Password.StartsWith('<');
}
