using STIN_BurzaModule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using STIN_BurzaModule.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<StockDataService>();
builder.Services.AddSingleton<FavoritesManager>();
builder.Services.AddSingleton<StockService>(); // Pøidáno zde

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Endpointy pøidány zde
app.MapGet("/new/rate", async (StockService stockService, CancellationToken stoppingToken) =>
{
    var items = await stockService.FetchStockData(stoppingToken);
    return items;
})
.WithName("GetItems")
.WithOpenApi();

app.MapGet("/filtered-items", async (StockService stockService, CancellationToken stoppingToken) =>
{
    var items = await stockService.FetchStockData(stoppingToken);
    var filteredItems = await stockService.FilterItems(items, stoppingToken);
    await stockService.SendToNewsModule(filteredItems, stoppingToken);
    return filteredItems;
})
.WithName("GetFilteredItems")
.WithOpenApi();

app.MapGet("/ui", () =>
{
    return Results.Content(@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Stock Module UI</title>
            <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
        </head>
        <body>
            <div class='container mt-5'>
                <h1>Stock Module</h1>
                <button class='btn btn-primary' onclick='fetchItems()'>Fetch Stock Data</button>
                <div id='results' class='mt-3'></div>
            </div>
            <script>
                async function fetchItems() {
                    const response = await fetch('/filtered-items');
                    const data = await response.json();
                    document.getElementById('results').innerHTML = JSON.stringify(data, null, 2);
                }
            </script>
        </body>
        </html>", "text/html");
})
.WithName("UI")
.WithOpenApi();

app.Run();

// Tøída StockDataService pøidána zde (po app.Run())
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