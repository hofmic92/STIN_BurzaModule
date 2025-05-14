using Microsoft.AspNetCore.Mvc;
using STIN_BurzaModule;
using STIN_BurzaModule.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Registrace služeb
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<StockDataBackgroundService>();
builder.Services.AddTransient<IndexModel>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<CommunicationManager>();
builder.Services.AddScoped<DeclineThreeDaysFilter>();
builder.Services.AddScoped<MoreThanTwoDeclinesFilter>();
builder.Services.AddScoped<FinalFilter>();
builder.Services.Configure<UserFilterSettings>(builder.Configuration.GetSection("UserFilters"));

// Build aplikace
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

// Endpointy
app.MapGet("/neco", () =>
{
    List<Item> items = new List<Item>
    {
        new Item("Microsoft", 1713960000, 0) { Rating = null, Sell = null }
    };
    return items;
})
.WithName("GetItems")
.WithOpenApi();

app.MapGet("/stock-data", async (HttpContext context) =>
{
    var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    const string apiKey = "LZGQZFV49DMW7M3J";
    var symbols = new List<string> { "AAPL", "MSFT", "GOOGL" };

    var items = new List<Item>();
    var client = httpClientFactory.CreateClient();

    foreach (var symbol in symbols)
    {
        try
        {
            string apiUrl = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiKey}";
            var response = await client.GetStringAsync(apiUrl);
            var json = System.Text.Json.JsonDocument.Parse(response).RootElement;
            var priceStr = json.GetProperty("Global Quote").GetProperty("05. price").GetString();
            if (decimal.TryParse(priceStr, out _))
            {
                items.Add(new Item(symbol, new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds(), 0));
                logger.LogInformation($"Successfully fetched {symbol}");
            }
            else
            {
                logger.LogWarning($"Failed to parse data for {symbol}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error fetching data for {symbol}");
        }
    }

    return Results.Json(items, new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new ItemJsonConverter() }
    });
})
.WithName("GetStockData")
.WithOpenApi();

app.MapPost("/send-stock-data", async (HttpContext context, IServiceProvider services) =>
{
    using var scope = services.CreateScope();
    var model = scope.ServiceProvider.GetRequiredService<IndexModel>();
    var url = "https://stinnews-cpaeakbfgkdpe0ae.westeurope-01.azurewebsites.net/UserDashboard";

    await model.FetchDataInternal();
    var items = model.GetStockItemsAsJson();
    var json = JsonSerializer.Serialize(items, new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new ItemJsonConverter() }
    });

    var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    var client = httpClientFactory.CreateClient();
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
        var response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return Results.Ok("Data successfully sent");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to send data: {ex.Message}");
    }
})
.WithName("SendStockData")
.WithOpenApi();

app.MapRazorPages();
app.Run();

// Supporting class
public class StockDay
{
    public DateTime Date { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Change { get; set; }
}