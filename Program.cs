using STIN_BurzaModule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using STIN_BurzaModule.Services;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<StockDataService>();
builder.Services.AddSingleton<FavoritesManager>();
builder.Services.AddSingleton<StockService>();
builder.Services.AddSingleton<StateManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection(); //odkomentovat pøi použití https !!!!!!!!!!!!!!!!

// Pøidání middleware pro zpracování výjimek
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        if (exception != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exception, "An unhandled exception occurred.");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("An unexpected error occurred. Please try again later.");
        }
    });
});

app.UseRouting();

// Endpoint pro získání dat (surových nebo filtrovaných)
app.MapGet("/data", async (StockService stockService, HttpRequest request, CancellationToken stoppingToken) =>
{
    bool applyFilter = request.Query.ContainsKey("filter") && bool.TryParse(request.Query["filter"], out bool filter) && filter;
    var items = await stockService.FetchStockData(stoppingToken);
    if (applyFilter)
    {
        var filteredItems = await stockService.FilterItems(items, stoppingToken);
        await stockService.SendToNewsModule(filteredItems, stoppingToken, Guid.NewGuid().ToString());
        return filteredItems;
    }
    return items;
})
.WithName("GetData")
.WithOpenApi();

// Endpoint pro UI (vèetnì správy oblíbených, nastavení filtrù a zobrazení zpráv)
app.MapGet("/ui", async (FavoritesManager favoritesManager, StockService stockService, IConfiguration config, ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    var favorites = favoritesManager.GetFavorites();
    var declineDays = config.GetValue<int>("UserSettings:DeclineDays", 3);
    var maxDeclines = config.GetValue<int>("UserSettings:MaxDeclines", 2);
    var maxNewsItems = config.GetValue<int>("NewsApi:MaxNewsItems", 50);

    // Volání News modulu pro zprávy
    var client = httpClientFactory.CreateClient();
    var listStockUrl = config["NewsModule:ListStockEndpoint"] ?? "http://localhost:8000/liststock";
    var newsResponse = await client.GetAsync(listStockUrl);
    var newsItems = newsResponse.IsSuccessStatusCode
        ? JsonSerializer.Deserialize<List<object>>(await newsResponse.Content.ReadAsStringAsync()) ?? new List<object>()
        : new List<object>
          {
              new { Company = "Microsoft", News = "Positive earnings report", Rating = 8 },
              new { Company = "Google", News = "Legal issues", Rating = -5 },
              new { Company = "Apple", News = "Neutral update", Rating = 0 }
          }.Take(maxNewsItems).ToList();

    var negativeNews = newsItems.Where(n => (int)n.GetType().GetProperty("Rating").GetValue(n) < 0).ToList();
    if (negativeNews.Count < config.GetValue<int>("UserSettings:MinNewsCount", 3))
    {
        logger.LogWarning("Not enough negative news available.");
    }

    // Zpracování akcí pro pøidání/odebrání oblíbených (stejné jako døív)
    var action = config["Favorites:Action"];
    var companyName = config["Favorites:CompanyName"];
    if (action == "add" && !string.IsNullOrEmpty(companyName))
    {
        favoritesManager.AddFavorite(companyName);
        favorites = favoritesManager.GetFavorites();
    }
    else if (action == "remove" && !string.IsNullOrEmpty(companyName))
    {
        favoritesManager.RemoveFavorite(companyName);
        favorites = favoritesManager.GetFavorites();
    }

    // Vygenerování HTML (stejné jako døív, jen s aktualizovanými zprávami)
    return Results.Content($@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Stock Module UI</title>
            <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
            <style>
                body {{ font-family: Arial, sans-serif; }}
                .container {{ max-width: 100%; padding: 10px; }}
                @media (max-width: 768px) {{ h1 {{ font-size: 1.5em; }} .btn {{ font-size: 0.8em; }} }}
            </style>
        </head>
        <body>
            <div class='container mt-3'>
                <h1>Stock Module</h1>
                <div class='row'>
                    <div class='col-md-6'>
                        <h3>Favorites</h3>
                        <ul id='favorites-list'>" +
                        string.Join("", favorites.Select(f => $"<li>{f} <button onclick=\"removeFavorite('{f}')\" class='btn btn-danger btn-sm'>Remove</button></li>")) +
                        $@"</ul>
                        <input type='text' id='new-favorite' placeholder='Add new favorite'>
                        <button class='btn btn-primary' onclick='addFavorite()'>Add Favorite</button>
                    </div>
                    <div class='col-md-6'>
                        <h3>Filters</h3>
                        <div>
                            <label>Decline Days: </label>
                            <input type='number' id='decline-days' value='{declineDays}' min='1'>
                        </div>
                        <div>
                            <label>Max Declines: </label>
                            <input type='number' id='max-declines' value='{maxDeclines}' min='0'>
                        </div>
                        <button class='btn btn-primary mt-2' onclick='fetchData(true)'>Fetch Filtered Data</button>
                        <button class='btn btn-primary mt-2' onclick='fetchData(false)'>Fetch Raw Data</button>
                        <div id='results' class='mt-3'></div>
                        <h3>News</h3>
                        <div id='news-items'>" +
                        string.Join("", newsItems.Select(item => $"<p><strong>{item.GetType().GetProperty("Company").GetValue(item)}</strong>: {item.GetType().GetProperty("News").GetValue(item)} (Rating: {item.GetType().GetProperty("Rating").GetValue(item)})</p>")) +
                        $@"</div>
                    </div>
                </div>
            </div>
            <script>
                async function fetchData(applyFilter) {{
                    const response = await fetch('/data' + (applyFilter ? '?filter=true' : ''));
                    const data = await response.json();
                    document.getElementById('results').innerHTML = JSON.stringify(data, null, 2);
                }}

                async function addFavorite() {{
                    const company = document.getElementById('new-favorite').value;
                    if (company) {{
                        window.location.href = '/ui?action=add&companyName=' + encodeURIComponent(company);
                    }}
                }}

                async function removeFavorite(company) {{window.location.href = '/ui?action=remove&companyName=' + encodeURIComponent(company);
                }}
            </script>
        </body>
        </html>", "text/html");
})
.WithName("UI")
.WithOpenApi();

app.Run();

// Tøídy definované po app.Run()
public class FavoriteRequest
{
    public required string CompanyName { get; set; }
}

public class FilterRequest
{
    public int DeclineDays { get; set; }
    public int MaxDeclines { get; set; }
}

public class StockDataService : BackgroundService
{
    private readonly StockService _stockService;

    public StockDataService(StockService stockService)
    {
        _stockService = stockService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var targetTimes = new[] { new TimeSpan(0, 0, 0), new TimeSpan(6, 0, 0), new TimeSpan(12, 0, 0), new TimeSpan(18, 0, 0) };
            var nextRun = targetTimes.OrderBy(t => t > now.TimeOfDay ? t : t + TimeSpan.FromDays(1)).First();

            var delay = (nextRun > now.TimeOfDay ? nextRun - now.TimeOfDay : nextRun + TimeSpan.FromDays(1) - now.TimeOfDay);
            await Task.Delay(delay, stoppingToken);

            await _stockService.FetchAndProcessStockData(stoppingToken);
        }
    }
}