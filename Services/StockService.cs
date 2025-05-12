using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
//CELÝ TENTO SOUBOR JE POTŘEBA UPRAVIT
namespace STIN_BurzaModule.Services
{
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
                        if (string.IsNullOrEmpty(item.getName()) || item.getRating().HasValue && (item.getRating() < -10 || item.getRating() > 10) || item.getSell().HasValue && (item.getSell() != 0 && item.getSell() != 1))
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

            foreach (var item in items ?? new List<Item>())
            {
                var historicalDataUrl = $"{_config["StockApi:BaseUrl"]}/{item.getName()}/history?days={_config.GetValue<int>("StockApi:HistoryDays", 10)}";
                try
                {
                    var response = await client.GetAsync(historicalDataUrl, stoppingToken);
                    response.EnsureSuccessStatusCode();
                    var historicalData = JsonSerializer.Deserialize<List<StockPrice>>(await response.Content.ReadAsStringAsync()) ?? new List<StockPrice>();

                    bool declinedLast3Days = historicalData.TakeLast(3).All(d => d.PriceChange < 0);
                    int declinesInLast5Days = historicalData.Count(d => d.PriceChange < 0);

                    if (declinedLast3Days || declinesInLast5Days > 2)
                    {
                        filteredItems.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to fetch historical data for {item.getName()}.");
                }
            }
            return filteredItems;
        }

        public async Task SendToNewsModule(List<Item> items, CancellationToken stoppingToken)
        {
            var client = _httpClientFactory.CreateClient();
            var newsModuleUrl = _config["NewsModule:RatingEndpoint"] ?? "http://partner:8000/rating";

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(items ?? new List<Item>()), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(newsModuleUrl, content, stoppingToken);
                response.EnsureSuccessStatusCode();

                var ratedItems = JsonSerializer.Deserialize<List<Item>>(await response.Content.ReadAsStringAsync()) ?? new List<Item>();
                var userRatingThreshold = _config.GetValue<int>("UserSettings:RatingThreshold", 5);

                foreach (Item item in ratedItems) //tady to bude potřeba překopat
                {
                    item.setSell();
                }
                var itemsToSell = ratedItems.Where(i => i.getSell() ==1 ).ToList();
                

                

                var sellEndpoint = _config["NewsModule:SaleStockEndpoint"] ?? "http://partner:8000/salestock";
                var sellContent = new StringContent(JsonSerializer.Serialize(itemsToSell), System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync(sellEndpoint, sellContent, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to communicate with News module.");
            }
        }

        // Přidána metoda FetchAndProcessStockData
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
}