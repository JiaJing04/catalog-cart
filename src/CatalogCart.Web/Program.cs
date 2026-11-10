using System.Net.Http.Headers;
using System.Text;
using CatalogCart.Web.Api;
using CatalogCart.Web.Components;
using CatalogCart.Web.Data;
using CatalogCart.Web.Options;
using CatalogCart.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration -----------------------
builder.Services.Configure<BoostOrderApiOptions>(
    builder.Configuration.GetSection(BoostOrderApiOptions.SectionName));
builder.Services.Configure<StoreOptions>(
    builder.Configuration.GetSection(StoreOptions.SectionName));

// ---- Local database (SQLite) -------------
var connectionString = builder.Configuration.GetConnectionString("CatalogCartDb")
    ?? "Data Source=App_Data/catalogcart.db";
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));

// ---- BoostOrder REST API ------------
// Named client + IHttpClientFactory so the singleton API client never holds on to a stale handler.
builder.Services.AddHttpClient(ProductApiClient.HttpClientName, (sp, http) =>
{
    var api = sp.GetRequiredService<IOptions<BoostOrderApiOptions>>().Value;
    http.Timeout = TimeSpan.FromSeconds(api.TimeoutSeconds);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    if (api.IsConfigured)
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.Username}:{api.Password}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
    }
});

// ---- App services ---------------------------------
// Singletons: this is a single-user demo, so one catalog snapshot and one cart are shared app-wide.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProductApiClient, ProductApiClient>();
builder.Services.AddSingleton<IProductCache, SqliteProductCache>();
builder.Services.AddSingleton<CatalogService>();
builder.Services.AddSingleton<CartService>();
builder.Services.AddSingleton<MoneyFormatter>();

// ---- Blazor ---------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Create the SQLite file and tables on first run.
await using (var db = await app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync())
{
    await db.Database.EnsureCreatedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
