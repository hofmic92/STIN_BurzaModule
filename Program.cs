using STIN_BurzaModule;
using StockModule.Pages;

var builder = WebApplication.CreateBuilder(args);

// Registrace služeb
builder.Services.AddRazorPages();
builder.Services.AddHttpClient(); // Nutné pro HttpClient v IndexModel
builder.Services.AddHostedService<StockDataBackgroundService>();
// Build aplikace (POUZE JEDNOU!)
var app = builder.Build();

// Konfigurace middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Váš endpoint pro akcie
app.MapGet("/new/rate", () =>
{
    List<Item> items = new List<Item>
    {
        new Item { Name = "Microsoft", Date = 1713960000, Rating = null, Sell = null },
        // ... (ostatní položky)
    };
    return items;
})
.WithName("GetItems")
.WithOpenApi();

app.MapRazorPages();

app.Run(); // Spuštění aplikace