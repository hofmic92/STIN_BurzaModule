using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace STIN_BurzaModule.Services;

public class StockService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StockService> _logger;
    private readonly IConfiguration _config;

    public StockService(IHttpClientFactory httpClientFactory, ILogger<StockService> logger, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task<List<Item>> FetchStockData(CancellationToken stoppingToken)
    {
        var client = _httpClientFactory.CreateClient();
        var externalApiUrl = _config["StockApi:BaseUrl"] ?? "https://api.example.com/stocks";
        int retries = _config.GetValue<int>("StockApi:RetryCount", 5);
        int delaySeconds = _config.GetValue<int>("StockApi:RetryDelaySeconds", 2);

        for (int i = 0; i < retries; i++)
        {
            try
            {
                var response = await client.GetAsync(externalApiUrl, stoppingToken);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<Item>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var validItems = new List<Item>();
                foreach (var item in items ?? new List<Item>())
                {
                    if (string.IsNullOrEmpty(item.Name) || item.Rating.HasValue && (item.Rating < -10 || item.Rating > 10) || item.Sell.HasValue && (item.Sell != 0 && item.Sell != 1))
                    {
                        _logger.LogWarning($"Invalid item data: {JsonSerializer.Serialize(item)}. Ignoring.");
                        continue;
                    }
                    validItems.Add(item);
                }
                return validItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch stock data. Retry {i + 1}/{retries}.");
                if (i == retries - 1) throw;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }
        return new List<Item>();
    }

    public async Task<List<Item>> FilterItems(List<Item> items, CancellationToken stoppingToken)
    {
        var filteredItems = new List<Item>();
        var client = _httpClientFactory.CreateClient();

        int declineDays = _config.GetValue<int>("UserSettings:DeclineDays", 3);
        int maxDeclines = _config.GetValue<int>("UserSettings:MaxDeclines", 2);
        int historyDays = _config.GetValue<int>("StockApi:HistoryDays", 10);

        foreach (var item in items ?? new List<Item>())
        {
            var historicalDataUrl = $"{_config["StockApi:BaseUrl"]}/{item.Name}/history?days={historyDays}";
            try
            {
                var response = await client.GetAsync(historicalDataUrl, stoppingToken);
                response.EnsureSuccessStatusCode();
                var historicalData = JsonSerializer.Deserialize<List<StockPrice>>(await response.Content.ReadAsStringAsync()) ?? new List<StockPrice>();

                var workingDaysData = historicalData
                    .Where(d => d.Date.DayOfWeek != DayOfWeek.Saturday && d.Date.DayOfWeek != DayOfWeek.Sunday)
                    .OrderByDescending(d => d.Date)
                    .ToList();

                var last5WorkingDays = workingDaysData.Take(5).ToList();
                var last3WorkingDays = workingDaysData.Take(declineDays).ToList();

                bool declinedLastNDays = last3WorkingDays.All(d => d.PriceChange < 0);
                int declinesInLastNDays = last5WorkingDays.Count(d => d.PriceChange < 0);

                if (declinedLastNDays || declinesInLastNDays > maxDeclines)
                {
                    filteredItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch historical data for {item.Name}.");
            }
        }
        return filteredItems;
    }

    public async Task SendToNewsModule(List<Item> items, CancellationToken stoppingToken, string clientId = null)
    {
        var client = _httpClientFactory.CreateClient();
        var newsModuleUrl = _config["NewsModule:RatingEndpoint"] ?? "http://localhost:8000/rating";

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(items ?? new List<Item>()), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(newsModuleUrl, content, stoppingToken);
            response.EnsureSuccessStatusCode();

            var ratedItems = JsonSerializer.Deserialize<List<Item>>(await response.Content.ReadAsStringAsync()) ?? new List<Item>();
            var userRatingThreshold = _config.GetValue<int>("UserSettings:RatingThreshold", 5);
            var defaultBuyAmount = _config.GetValue<int>("UserSettings:DefaultBuyAmount", 1);

            var favoritesManager = new FavoritesManager(_config);
            var favorites = favoritesManager.GetFavorites();

            var stateManager = new StateManager();
            stateManager.Enqueue(clientId ?? Guid.NewGuid().ToString(), ratedItems);

            var itemsToSell = new List<Item>();
            var itemsToBuy = new List<Item>();

            foreach (var item in ratedItems)
            {
                if (!favorites.Contains(item.Name))
                {
                    _logger.LogWarning($"Received rating for unknown company {item.Name}. Ignoring.");
                    continue;
                }

                if (item.Rating > userRatingThreshold)
                {
                    item.Sell = 1;
                    itemsToSell.Add(item);
                }
                else if (item.Sell == null || item.Sell == 0)
                {
                    itemsToBuy.Add(new Item { Name = item.Name, Date = item.Date, Rating = item.Rating, Sell = 0 });
                    _logger.LogInformation($"Buying {defaultBuyAmount} shares of {item.Name} as no sell recommendation exists.");
                }
            }

            var sellEndpoint = _config["NewsModule:SaleStockEndpoint"] ?? "http://localhost:8000/salestock";
            var sellContent = new StringContent(JsonSerializer.Serialize(itemsToSell), System.Text.Encoding.UTF8, "application/json");
            await client.PostAsync(sellEndpoint, sellContent, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with News module.");
        }
    }

    public async Task FetchAndProcessStockData(CancellationToken stoppingToken)
    {
        var items = await FetchStockData(stoppingToken);
        var filteredItems = await FilterItems(items, stoppingToken);
        await SendToNewsModule(filteredItems, stoppingToken);
    }

    public class StockPrice
    {
        public DateTime Date { get; set; }
        public decimal PriceChange { get; set; }
    }
}