namespace CatalogCart.Web.Options;

/// <summary>Display settings for the header and prices. loaded from the "Store" section.</summary>
public sealed class StoreOptions
{
    public const string SectionName = "Store";

    public string CompanyName { get; set; } = "Company Name";

    public string CatalogTitle { get; set; } = "Categories Name";

    public string CurrencySymbol { get; set; } = "RM";

    /// <summary>"Company Name" becomes "CN" for the avatar circle in the header.</summary>
    public string CompanyInitials
    {
        get
        {
            var letters = CompanyName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]))
                .Take(2);
            return string.Concat(letters);
        }
    }
}
