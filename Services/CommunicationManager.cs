using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using STIN_BurzaModule;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using STIN_BurzaModule.DataModel;

namespace STIN_BurzaModule.Services
{
    public class CommunicationManager
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CommunicationManager> _logger;
        private readonly IConfiguration _config;
        private readonly DeclineThreeDaysFilter _declineThreeDaysFilter;
        private readonly MoreThanTwoDeclinesFilter _moreThanTwoDeclinesFilter;
        private readonly FinalFilter _finalFilter;
        private readonly UserFilterSettings _userFilterSettings;
        private readonly string _newsUrl;
        private readonly int _maxRetries;
        private readonly int _retryDelaySeconds;
        private readonly int _timeoutSeconds;

        public CommunicationManager(
            IHttpClientFactory httpClientFactory,
            ILogger<CommunicationManager> logger,
            IConfiguration config,
            DeclineThreeDaysFilter declineThreeDaysFilter,
            MoreThanTwoDeclinesFilter moreThanTwoDeclinesFilter,
            FinalFilter finalFilter,
            UserFilterSettings userFilterSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = config;
            _declineThreeDaysFilter = declineThreeDaysFilter;
            _moreThanTwoDeclinesFilter = moreThanTwoDeclinesFilter;
            _finalFilter = finalFilter;
            _userFilterSettings = userFilterSettings;

            _newsUrl = _config["Communication:NewsUrl"] ?? "http://partner:8000";
            _maxRetries = _config.GetValue<int>("Communication:Retry:MaxAttempts", 5);
            _retryDelaySeconds = _config.GetValue<int>("Communication:Retry:DelaySeconds", 2);
            _timeoutSeconds = _config.GetValue<int>("Communication:TimeoutSeconds", 10);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
        }

        public async Task<List<Item>> ProcessStocksAsync(string jsonData, CancellationToken stoppingToken)
        {
            // Krok 1: Přijmu JSON a převedu na List<Item> s validací
            List<Item> items = DeserializeJson(jsonData);
            if (items == null || items.Count == 0)
            {
                _logger.LogWarning("Žádné platné položky v JSON datu.");
                return new List<Item>();
            }

            // Krok 2: Filtrování podle uživatelských nastavení
            var filteredItems = items;
            if (_userFilterSettings.EnableDeclineThreeDays)
            {
                filteredItems = _declineThreeDaysFilter.filter(filteredItems);
                _logger.LogInformation("Aplikace filtru DeclineThreeDaysFilter.");
            }
            if (_userFilterSettings.EnableMoreThanTwoDeclines)
            {
                filteredItems = _moreThanTwoDeclinesFilter.filter(filteredItems);
                _logger.LogInformation("Aplikace filtru MoreThanTwoDeclinesFilter.");
            }
            if (filteredItems == null || filteredItems.Count == 0)
            {
                _logger.LogWarning("Žádné položky po filtrování.");
                return new List<Item>();
            }

            // Krok 3: Pošlu filtrovaná data do News modulu (liststock)
            var liststockResponse = await SendToNewsAsync(
                _config["Communication:Endpoints:ListStock"] ?? "/liststock",
                filteredItems,
                stoppingToken
            );
            if (liststockResponse == null)
            {
                _logger.LogError("Chyba při odesílání dat do News modulu (liststock).");
                return new List<Item>();
            }

            // Krok 4: Zpracování - výběr nejnovějších záznamů (FinalFilter)
            var processedItems = _finalFilter.filter(liststockResponse);
            if (processedItems == null || processedItems.Count == 0)
            {
                _logger.LogWarning("Žádné položky po zpracování (FinalFilter).");
                return new List<Item>();
            }

            // Krok 5: Pošlu zpracovaná data do News modulu (salestock)
            var salestockResponse = await SendToNewsAsync(
                _config["Communication:Endpoints:SaleStock"] ?? "/salestock",
                processedItems,
                stoppingToken
            );
            if (salestockResponse == null)
            {
                _logger.LogError("Chyba při odesílání dat do News modulu (salestock).");
                return new List<Item>();
            }

            _logger.LogInformation("Komunikace s News modulem úspěšně dokončena.");
            return salestockResponse;
        }

        private List<Item> DeserializeJson(string jsonData)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var rawItems = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonData, options) ?? new List<Dictionary<string, object>>();
                var items = new List<Item>();

                foreach (var rawItem in rawItems)
                {
                    if (TryParseItem(rawItem, out var item))
                    {
                        items.Add(item);
                    }
                    else
                    {
                        _logger.LogWarning($"Neplatná položka ignorována: {JsonSerializer.Serialize(rawItem)}");
                    }
                }
                return items;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Chyba při deserializaci JSON dat.");
                return new List<Item>();
            }
        }

        private bool TryParseItem(Dictionary<string, object> data, out Item item)
        {
            item = null;
            try
            {
                string name = data.ContainsKey("name") ? data["name"].ToString() : null;
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

        private async Task<List<Item>> SendToNewsAsync(string endpoint, List<Item> items, CancellationToken stoppingToken)
        {
            var url = $"{_newsUrl}{endpoint}";
            var client = _httpClientFactory.CreateClient();
            var serializedItems = JsonSerializer.Serialize(items, new JsonSerializerOptions
            {
                WriteIndented = true,
               // Converters = { new ItemJsonConverter() }
            });
            var content = new StringContent(serializedItems, System.Text.Encoding.UTF8, "application/json");

            for (int attempt = 0; attempt < _maxRetries; attempt++)
            {
                try
                {
                    var response = await client.PostAsync(url, content, stoppingToken);
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return DeserializeJson(responseContent);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, $"Pokus {attempt + 1}/{_maxRetries} selhal pro {url}.");
                    if (attempt + 1 == _maxRetries)
                    {
                        _logger.LogError("Dosaženo maximálního počtu pokusů.");
                        return null;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(_retryDelaySeconds), stoppingToken);
                }
            }
            return null;
        }
    }

    // Vlastní konvertor pro serializaci Item s getterovou strukturou
    /*public class ItemJsonConverter : System.Text.Json.Serialization.JsonConverter<Item>
    {
        public override Item ReadJson(JsonSerializer reader, Type objectType, Item existingValue, bool hasValue, JsonSerializerOptions options)
        {
            return null; // Deserializace je řešena v TryParseItem
        }

        public override void WriteJson(Utf8JsonWriter writer, Item value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("name", value.getName());
            writer.WriteNumber("date", value.getDate());
            if (value.getRating().HasValue) writer.WriteNumber("rating", value.getRating().Value);
            if (value.getSell().HasValue) writer.WriteNumber("sell", value.getSell().Value);
            writer.WriteEndObject();
        }
    }*/
}