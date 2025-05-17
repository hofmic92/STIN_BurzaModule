using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using STIN_BurzaModule;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using STIN_BurzaModule.DataModel;
using System.Text.Json;

namespace STIN_BurzaModule.Services;

public class StockService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StockService> _logger;
    private readonly IConfiguration _config;
    private readonly CommunicationManager _communicationManager;

    public StockService(
        IHttpClientFactory httpClientFactory,
        ILogger<StockService> logger,
        IConfiguration config,
        CommunicationManager communicationManager)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
        _communicationManager = communicationManager;
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
                var items = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Dictionary<string, object>>();

                var validItems = new List<Item>();
                foreach (var itemData in items)
                {
                    if (TryParseItem(itemData, out var item))
                    {
                        validItems.Add(item);
                    }
                    else
                    {
                        _logger.LogWarning($"Neplatná položka: {System.Text.Json.JsonSerializer.Serialize(itemData)}. Ignorování.");
                    }
                }
                return validItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Chyba při načítání dat. Pokus {i + 1}/{retries}.");
                if (i == retries - 1) throw;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }
        return new List<Item>();
    }

    private bool TryParseItem(Dictionary<string, object> data, out Item item)
    {
        item = null!; //pokus o vyřešení varování v githubu
        try
        {
            string name = data.ContainsKey("name") ? (data["name"]?.ToString() ?? "") : ""; //pokus o vyřešení varování v githubu
            if (string.IsNullOrEmpty(name)) return false;

            long date = data.ContainsKey("date") && long.TryParse(data["date"].ToString(), out var d) ? d : 0;
            if (date <= 0) return false;

            int price = data.ContainsKey("price") && int.TryParse(data["price"].ToString(), out var p) ? p : 0;

            int? rating = data.ContainsKey("rating") && int.TryParse(data["rating"].ToString(), out var r) ? r : (int?)null;
            if (rating.HasValue && (rating < -10 || rating > 10)) return false;

            int? sell = data.ContainsKey("sell") && int.TryParse(data["sell"].ToString(), out var s) ? s : (int?)null;
            if (sell.HasValue && sell != 0 && sell != 1) return false;

            item = new Item(name, date, price);
            if (rating.HasValue) item.setRating(rating.Value);
            if (sell.HasValue) item.setSellValue(sell.Value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Item>> ProcessStockData(CancellationToken stoppingToken)
    {
        var items = await FetchStockData(stoppingToken);
        if (items.Count == 0)
        {
            _logger.LogWarning("Žádné položky k zpracování.");
            return new List<Item>();
        }

        var jsonData = System.Text.Json.JsonSerializer.Serialize(items, new JsonSerializerOptions
        {
            WriteIndented = true,
            //Converters = { new ItemJsonConverter() }
        });
        return await _communicationManager.ProcessStocksAsync(jsonData, stoppingToken);
    }
}