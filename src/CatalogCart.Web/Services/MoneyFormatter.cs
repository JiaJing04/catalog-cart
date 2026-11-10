using System.Globalization;
using CatalogCart.Web.Options;
using Microsoft.Extensions.Options;

namespace CatalogCart.Web.Services;

/// <summary>Formats prices like the mockup: "RM 100.00".</summary>
public sealed class MoneyFormatter(IOptions<StoreOptions> options)
{
    private readonly string _symbol = options.Value.CurrencySymbol;

    public string Format(decimal amount) =>
        $"{_symbol} {Math.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("N2", CultureInfo.InvariantCulture)}";
}
