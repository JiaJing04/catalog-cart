# Catalog & Cart

A web app with a product catalog and a cart, built for the .NET assessment (chosen from the second document sent by the HR: •	Web application – ASP.NET Core / Blazor).

It pulls products from the BoostOrder REST API, keeps a local SQLite copy so the catalog
works offline, and follows the supplied mockups.




## Running it

**Requirements:** the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

### 1. Add the API settings

`appsettings.json` only holds placeholders. Keep the real values out of source control with user-secrets:

```bash
cd src/CatalogCart.Web
dotnet user-secrets set "BoostOrderApi:ProductsUrl" "https://<host>/.../wp-json/wc/v1/bo/products"
dotnet user-secrets set "BoostOrderApi:Username"    "<consumer key>"
dotnet user-secrets set "BoostOrderApi:Password"    "<consumer secret>"
```


### 2. Run

```bash
dotnet run --project src/CatalogCart.Web
```

Open the URL it prints (e.g. `http://localhost:5244`).

The SQLite file is created at `src/CatalogCart.Web/App_Data/catalogcart.db` on first run.
Delete it to start fresh.

### Seeing offline mode

1. Run the app with internet access. The catalog downloads and is saved to SQLite.
2. Turn off Wi-Fi or unplug the network cable.
3. Refresh the page, or choose refresh products.
4. Reconnect and tap **Retry**. The live data comes back.

## Requirements

| Requirement | Type | Done |
|---|---|---|
| Fetch products from API | Mandatory | Done |
| Show only type variable | Mandatory | Done |
| Works offline (SQLite fallback) | Mandatory | Done |
| Cart row: unit price, qty, total | Mandatory | Done |
| Footer: line count + cart total | Mandatory | Done |
| Swipe to remove product | Mandatory | Done |
| Clear cart from menu | Mandatory | Done |
| Checkout button | Skipped | Skipped |
| Cart badge icon | Bonus | done |
| UOM dropdown changes price/stock | Bonus | Done |
| Product search | Bonus | Done |


---

## Project structure

```
src/CatalogCart.Web/
├── Program.cs                 Dependency injection, HttpClient + Basic Auth, SQLite, Blazor
├── appsettings.json           Placeholders only (see "Running it")
├── Options/                   Typed settings: BoostOrderApiOptions, StoreOptions
├── Api/                       Everything that knows the API's JSON shape
│   ├── Dtos/ProductDto.cs     Payload classes
│   ├── Json/                  Lenient number/bool parsing ("100.00", "", null)
│   ├── ProductMapper.cs       Payload classes → app models (incl. UOM label)
│   └── ProductApiClient.cs    Paging loop, unwraps { "products": [...] }
├── Models/                    Product, ProductVariation (UOM), CartItem, CatalogSnapshot
├── Data/                      AppDbContext (SQLite) and SqliteProductCache
├── Services/                  CatalogService (online/offline), CartService, MoneyFormatter
├── Components/
│   ├── Pages/                 CatalogPage (/), CartPage (/cart)
│   ├── Catalog/               ProductRow
│   ├── Cart/                  CartLineRow
│   └── Shared/                AppHeader, CartButton, QuantityStepper, StockLabel, ...
└── wwwroot/                   app.css (mockup styles), js/swipe.js, img/placeholder.png
```

Dependencies only point one way: **Components to Services to (Api, Data) to Models**.

---

## Design decisions (and why)

- **Offline fallback to a local SQLite copy.** `CatalogService` uses the in-memory
  catalog first, then the API, then SQLite. Each successful download replaces the cached
  catalog inside one transaction, so the catalog still works without internet and a
  failed save leaves the previous copy intact.
- **One-way layers.** Dependencies only point one way: Components → Services →
  (Api, Data) → Models. The API's JSON shape is kept in `Api/` (payload classes plus a
  mapper), so the UI never deals with raw API data.
- **`decimal` for all money, totals always calculated.** Line totals are
  `UnitPrice × Quantity` and the cart total is their sum. Nothing is stored, so totals
  can't drift and there are no rounding errors.

**Known limitations (this is a simple demo only, not production-ready):**

- The offline fallback only covers the product API being unavailable. The app runs on the
  server, so the browser still needs a connection. Working offline on the device would need
  a WebAssembly PWA or MAUI.
- The cart is shared by all users (singleton). A real app would keep a separate cart for each user.
