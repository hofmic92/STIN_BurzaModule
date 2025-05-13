using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace StockModule.Pages
{
    public class IndexModel : PageModel
    {
        private static List<string> _favoriteItems = new();
        private static StringBuilder _logBuilder = new();
        private static Dictionary<string, decimal> _stockPrices = new();
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiKey = "LZGQZFV49DMW7M3J"; // Nahraď skutečným klíčem (Alpha Vantage/Yahoo Finance)

        [BindProperty]
        public string? NewItem { get; set; }

        public List<string> FavoriteItems => _favoriteItems;
        public string LogOutput => _logBuilder.ToString();
        public Dictionary<string, decimal> StockPrices => _stockPrices;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public void OnGet()
        {
            Log("Načtení stránky Index");
        }

        public async Task<IActionResult> OnPostFetchData()
        {
            await FetchDataInternal();
            return RedirectToPage();
        }

        public DateTime LastUpdateTime { get; private set; }

        internal async Task FetchDataInternal()
        {
            if (_favoriteItems.Count == 0)
            {
                Log("Žádné položky ke stažení");
                return;
            }

            var startTime = DateTime.Now;
            Log($"Spouštím stahování v {startTime:HH:mm:ss}");

            foreach (var symbol in _favoriteItems)
            {
                try
                {
                    var price = await FetchStockPrice(symbol);
                    if (price > 0)
                    {
                        _stockPrices[symbol] = price;
                        Log($"Úspěch: {symbol} - {price:C}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Chyba u {symbol}: {ex.Message}");
                }
            }

            LastUpdateTime = DateTime.Now;
            Log($"Stahování dokončeno v {LastUpdateTime:HH:mm:ss}");
        }

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
            return RedirectToPage();
        }

        public IActionResult OnPostRemoveItem(string item)
        {
            if (!string.IsNullOrEmpty(item) && _favoriteItems.Contains(item))
            {
                _favoriteItems.Remove(item);
                Log($"Položka odebrána: {item}");
            }
            return RedirectToPage();
        }

        public IActionResult OnPostClearLog()
        {
            _logBuilder.Clear();
            Log("Log byl vymazán");
            return RedirectToPage();
        }


        private void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"{timestamp} | {message}";
            _logBuilder.AppendLine(logEntry);

            // Omezení logu na posledních 100 zpráv
            var lines = _logBuilder.ToString().Split(Environment.NewLine);
            if (lines.Length > 100)
            {
                _logBuilder = new StringBuilder(string.Join(Environment.NewLine, lines.Skip(lines.Length - 100)));
            }
        }
    }
}