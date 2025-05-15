using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using STIN_BurzaModule;
using STIN_BurzaModule.DataModel;
using STIN_BurzaModule.Filters;

namespace StockModule.Pages
{
    public class IndexModel : PageModel
    {
        private static List<string> _favoriteItems = new();
        private static StringBuilder _logBuilder = new();
        private static Dictionary<string, decimal> _stockPrices = new();
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiKey = "LZGQZFV49DMW7M3J";
        private static readonly string FavoritesFilePath = Path.Combine("Data", "favorites.json");
        private static readonly string LogFilePath = Path.Combine("Logs", "application.log");
        private static bool UseStaticData = true;
        public List<Item> DownloadedItems { get; private set; } = new();

        public DateTime LastUpdateTime { get; private set; }

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            InitializeLogging();
        }

        private void InitializeLogging()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
                if (!System.IO.File.Exists(LogFilePath))
                {
                    System.IO.File.Create(LogFilePath).Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize logging: {ex.Message}");
            }
        }
        /*public List<DataModel> GetStockItems()
        {
            var items = new List<DataModel>();

            foreach (var stock in _stockPrices)
            {
                items.Add(new DataModel
                {
                    Name = stock.Key,
                    Date = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds(),
                    Rating = 0,
                    Sell = 0
                });
            }

            return items;
        }*/

        public List<Item> GetStockItems()
        {
            long unixTime = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

            var items = new List<Item>();

            foreach (var kvp in _stockPrices)
            {
                string name = kvp.Key;
                decimal priceDecimal = kvp.Value;
                int price = (int)Math.Round(priceDecimal);

                var item = new Item(name, unixTime, price);
                items.Add(item);
            }

            return items;
        }


        [BindProperty]
        public string? NewItem { get; set; }

        public List<string> FavoriteItems => _favoriteItems;
        public string LogOutput => _logBuilder.ToString();
        public Dictionary<string, decimal> StockPrices => _stockPrices;

        public void OnGet()
        {
            LoadFavoritesFromFile();
            Log("Načtení stránky Index");
        }

        public async Task<IActionResult> OnPostFetchData()
        {
            await FetchDataInternal();
            return RedirectToPage();
        }


        internal async Task FetchDataInternal()
        {
            if (_favoriteItems.Count == 0)
            {
                Log("Žádné položky ke stažení");
            }

            var startTime = DateTime.Now;
            Log($"Spouštím stahování historických dat v {startTime:HH:mm:ss}");

            _stockPrices.Clear(); // nebudeš používat, pokud přecházíš na Itemy
            List<Item> historyItems;
            /*foreach (var symbol in _favoriteItems)
            {
                try
                {
                    historyItems = await FetchStockHistory(symbol, 5);
                    foreach (var item in historyItems)
                    {
                        Console.WriteLine($"DEBUG: Symbol={item.getName()}, Datum={item.getDate()}, Cena={item.getPrice()}");
                    }
                    Console.WriteLine("ooooo");

                    foreach (var item in historyItems)
                    {
                        Log($"✓ {symbol} | {item.getDate()} | {item.getPrice()}");
                        // Můžeš si zde ukládat nebo zpracovávat položky dle potřeby
                    }
                    DownloadedItems = historyItems;
                    foreach (var item in DownloadedItems)
                    {
                        Console.WriteLine($"DEBUG: Symbol={item.getName()}, Datum={item.getDate()}, Cena={item.getPrice()}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Chyba u {symbol}: {ex.Message}");
                }
            }*/
            DownloadedItems = new List<Item>(); // inicializace před cyklem

            foreach (var symbol in _favoriteItems)
            {
                try
                {
                    historyItems = await FetchStockHistory(symbol, 5);

                    foreach (var item in historyItems)
                    {
                        Console.WriteLine($"DEBUG: Symbol={item.getName()}, Datum={item.getDate()}, Cena={item.getPrice()}");
                    }

                    foreach (var item in historyItems)
                    {
                        Log($"✓ {symbol} | {item.getDate()} | {item.getPrice()}");
                    }

                    DownloadedItems.AddRange(historyItems); // správné přidání všech položek
                }
                catch (Exception ex)
                {
                    Log($"Chyba u {symbol}: {ex.Message}");
                }
            }


            LastUpdateTime = DateTime.Now;
            Log($"Stahování dokončeno v {LastUpdateTime:HH:mm:ss}");
        }

        

        private async Task<List<Item>> FetchStockHistory(string symbol, int daysBack)
        {
            /*if (UseStaticData)
            {
                var now = DateTime.Now;
                var staticItems = new List<Item>();
                for (int i = 0; i < daysBack; i++)
                {
                    var date = now.AddDays(-i);
                    long unixTimestamp = new DateTimeOffset(date).ToUnixTimeSeconds();
                    int price = 100 + i * 2; // nějaký vzorek růstu ceny

                    staticItems.Add(new Item(symbol, unixTimestamp, price));
                }
                return await Task.FromResult(staticItems);
            }*/
            if (UseStaticData)
            {
                var now = DateTime.Now;
                var staticItems = new List<Item>();

                bool isDescending = symbol == "MSFT"; // Nastav si, který symbol má klesat

                for (int i = 0; i < daysBack; i++)
                {
                    var date = now.AddDays(-i);
                    long unixTimestamp = new DateTimeOffset(date).ToUnixTimeSeconds();

                    int price;
                    if (isDescending)
                    {
                        price = 100 + (daysBack - i - 1) * 2; // 108, 106, 104, ...
                    }
                    else
                    {
                        price = 100 + i * 2; // 100, 102, 104, ...
                    }

                    staticItems.Add(new Item(symbol, unixTimestamp, price));
                }

                return await Task.FromResult(staticItems);
            }



            // skutečný API dotaz – ponecháme beze změny
            var client = _httpClientFactory.CreateClient();
            string apiUrl = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={ApiKey}";

            var response = await client.GetStringAsync(apiUrl);
            var json = JObject.Parse(response);

            var timeSeries = json["Time Series (Daily)"];
            if (timeSeries == null)
            {
                throw new Exception($"Chybí data TIME_SERIES_DAILY pro {symbol}");
            }

            var items = new List<Item>();

            foreach (var entry in timeSeries.Children<JProperty>().Take(daysBack))
            {
                string dateStr = entry.Name;
                var data = entry.Value;

                if (decimal.TryParse(data["4. close"]?.ToString(), out decimal closePrice))
                {
                    DateTime date = DateTime.Parse(dateStr);
                    long unixTimestamp = new DateTimeOffset(date).ToUnixTimeSeconds();
                    int price = (int)Math.Round(closePrice);

                    var item = new Item(symbol, unixTimestamp, price);
                    items.Add(item);
                }
            }

            return items;
        }


        /*private async Task<List<Item>> FetchStockHistory(string symbol, int daysBack)
        {
            var client = _httpClientFactory.CreateClient();
            string apiUrl = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={ApiKey}";

            var response = await client.GetStringAsync(apiUrl);
            var json = JObject.Parse(response);

            var timeSeries = json["Time Series (Daily)"];
            if (timeSeries == null)
            {
                throw new Exception($"Chybí data TIME_SERIES_DAILY pro {symbol}");
            }

            var items = new List<Item>();

            // vezmeme posledních N dní zpětně
            foreach (var entry in timeSeries.Children<JProperty>().Take(daysBack))
            {
                string dateStr = entry.Name;
                var data = entry.Value;

                if (decimal.TryParse(data["4. close"]?.ToString(), out decimal closePrice))
                {
                    DateTime date = DateTime.Parse(dateStr);
                    long unixTimestamp = new DateTimeOffset(date).ToUnixTimeSeconds();
                    int price = (int)Math.Round(closePrice);

                    var item = new Item(symbol, unixTimestamp, price);
                    items.Add(item);
                }
            }

            return items;
        }*/






        private async Task<decimal> FetchStockPrice(string symbol)
        {
            var client = _httpClientFactory.CreateClient();
            string apiUrl = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={ApiKey}";

            var response = await client.GetStringAsync(apiUrl);
            var json = JObject.Parse(response);

            var priceStr = json["Global Quote"]?["05. price"]?.ToString();

            if (decimal.TryParse(priceStr, out var price))
            {
                return price;
            }

            throw new Exception("Nelze parsovat cenu");
        }

        public IActionResult OnPostAddItem()
        {
            if (!string.IsNullOrWhiteSpace(NewItem))
            {
                var itemToAdd = NewItem.Trim().ToUpper();
                if (!_favoriteItems.Contains(itemToAdd))
                {
                    _favoriteItems.Add(itemToAdd);
                    Log($"Položka přidána: {itemToAdd}");
                }
            }
            SaveFavoritesToFile();
            return RedirectToPage();
        }

        public IActionResult OnPostRemoveItem(string item)
        {
            if (!string.IsNullOrEmpty(item) && _favoriteItems.Contains(item))
            {
                _favoriteItems.Remove(item);
                Log($"Položka odebrána: {item}");
            }
            SaveFavoritesToFile();
            return RedirectToPage();
        }

        public IActionResult OnPostClearLog()
        {
            _logBuilder.Clear();
            Log("Log byl vymazán");
            return RedirectToPage();
        }


        private void LoadFavoritesFromFile()
        {
            try
            {
                if (System.IO.File.Exists(FavoritesFilePath))
                {
                    var json = System.IO.File.ReadAllText(FavoritesFilePath);
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                    if (list != null)
                    {
                        _favoriteItems = list;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Chyba při načítání oblíbených: {ex.Message}");
            }
        }

        private void SaveFavoritesToFile()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_favoriteItems, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                Directory.CreateDirectory(Path.GetDirectoryName(FavoritesFilePath)!);
                System.IO.File.WriteAllText(FavoritesFilePath, json);
            }
            catch (Exception ex)
            {
                Log($"Chyba při ukládání oblíbených: {ex.Message}");
            }
        }


        private void Log(string message)
        {
            /*var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"{timestamp} | {message}";
            _logBuilder.AppendLine(logEntry);

            // Omezení logu na posledních 100 zpráv
            var lines = _logBuilder.ToString().Split(Environment.NewLine);
            if (lines.Length > 100)
            {
                _logBuilder = new StringBuilder(string.Join(Environment.NewLine, lines.Skip(lines.Length - 100)));
            }*/
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"{timestamp} | {message}";

            // Add to in-memory log
            _logBuilder.AppendLine(logEntry);

            // Limit in-memory log to last 100 messages
            var lines = _logBuilder.ToString().Split(Environment.NewLine);
            if (lines.Length > 100)
            {
                _logBuilder = new StringBuilder(string.Join(Environment.NewLine, lines.Skip(lines.Length - 100)));
            }

            // Write to file
            try
            {
                System.IO.File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}