using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using STIN_BurzaModule;
using StockModule.Pages;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Registrace služeb
builder.Services.AddRazorPages();
builder.Services.AddHttpClient(); // Nutné pro HttpClient v IndexModel
builder.Services.AddHostedService<StockDataBackgroundService>();
builder.Services.AddTransient<IndexModel>();
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
app.MapGet("/neco", () =>
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


app.MapGet("/stock-data", async (HttpContext context) =>
{
    // 1. Get required services
    var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    // 2. Configure API settings
    const string apiKey = "LZGQZFV49DMW7M3J"; // Replace with your actual API key
    var symbols = new List<string> { "AAPL", "MSFT", "GOOGL" }; // Default symbols

    // 3. Fetch stock data
    var items = new List<Item>();
    var client = httpClientFactory.CreateClient();

    foreach (var symbol in symbols)
    {
        try
        {
            string apiUrl = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiKey}";
            var response = await client.GetStringAsync(apiUrl);
            var json = JObject.Parse(response);

            var priceStr = json["Global Quote"]?["05. price"]?.ToString();
            if (decimal.TryParse(priceStr, out _))
            {
                items.Add(new Item
                {
                    Name = symbol,
                    Date = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds(),
                    Rating = null,  // You can add rating logic later
                    Sell = null     // You can add sell logic later
                });
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

    // 4. Return the data
    return Results.Json(items, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
})
.WithName("GetStockData")
.WithOpenApi(operation => new(operation)
{
    Summary = "Gets real stock data",
    Description = "Fetches current stock prices from Alpha Vantage API"
});

app.MapPost("/send-stock-data", async (HttpContext context, IServiceProvider services) =>
{
    using var scope = services.CreateScope();
    var model = scope.ServiceProvider.GetRequiredService<IndexModel>();

    var url = "https://stinnews-cpaeakbfgkdpe0ae.westeurope-01.azurewebsites.net/UserDashboard";


    // Fetch latest data
    await model.FetchDataInternal();

    // Get data in JSON format
    var items = model.GetStockItemsAsJson();
    string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);

    // Here you would send this data to your external endpoint
    // For example:
    var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    var client = httpClientFactory.CreateClient();

    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    var content = new StringContent(JsonSerializer.Serialize(items, options), Encoding.UTF8, "application/json");
    var response = await client.PostAsync(url, content);


    try
    {
        response = await client.PostAsJsonAsync(url, items);
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

app.Run(); // Spuštění aplikace

// Supporting classes
public class StockDay
{
    public DateTime Date { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Change { get; set; }
}

public class Item
{
    public string Name { get; set; }
    public long Date { get; set; }
    public int? Rating { get; set; }
    public int? Sell { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double[]? DailyChanges { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? FilterFlags { get; set; }
}



