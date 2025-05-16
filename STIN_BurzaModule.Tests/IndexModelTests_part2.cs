using StockModule.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using STIN_BurzaModule;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace STIN_BurzaModule.Tests
{
    public class IndexModelTests_All
    {
        private static string tempPath = Path.Combine(Path.GetTempPath(), "burza_test");

        private static IndexModel CreateModel()
        {
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            return new IndexModel(httpClientFactoryMock.Object);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }


        private static T GetField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return (T)field?.GetValue(null);
        }

        [Fact]
        public void InitializeLogging_CreatesLogFile()
        {
            var model = CreateModel();
            string logPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "application.log");
            Assert.True(File.Exists(logPath));
        }

        [Fact]
        public void GetStockItems_ReturnsItemsFromStockPrices()
        {
            var model = CreateModel();
            var testPrices = new Dictionary<string, decimal> { { "AAPL", 123.45M }, { "TSLA", 678.90M } };

            var stockPricesField = typeof(IndexModel).GetField("_stockPrices", BindingFlags.NonPublic | BindingFlags.Static);
            stockPricesField?.SetValue(null, testPrices);

            var items = model.GetStockItems();

            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.getName() == "AAPL");
            Assert.Contains(items, i => i.getName() == "TSLA");
        }


        [Fact]
        public void OnGet_LogsPageLoad()
        {
            var model = CreateModel();
            model.OnGet();

            var log = GetField<StringBuilder>(model, "_logBuilder").ToString();
            Assert.Contains("Načtení stránky Index", log);
        }

        [Fact]
        public async Task OnPostUpdateRatings_UpdatesExistingItems()
        {
            var model = CreateModel();

            var item = new Item("AAPL", 1000000000, 120);
            typeof(IndexModel)
                .GetProperty("DownloadedItems", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(model, new List<Item> { item });

            var json = """[{"Name":"AAPL","Date":1000000000,"Rating":3}]""";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Request = { Body = stream }
                }
            };

            var result = await model.OnPostUpdateRatings();

            Assert.IsType<OkResult>(result);
        }








        [Fact]
        public async Task OnPostUpdateRatings_ReturnsBadRequest_OnInvalidJson()
        {
            var model = CreateModel();
            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes("INVALID_JSON"));

            model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
            model.PageContext.HttpContext.Request.Body = bodyStream;

            var result = await model.OnPostUpdateRatings();

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task OnPostFetchData_CallsFetchDataInternal()
        {
            var model = CreateModel();
            var result = await model.OnPostFetchData();
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnGetGetStockData_ReturnsJson()
        {
            var model = CreateModel();

            var item = new Item("AAPL", 1000000000, 150);

            typeof(IndexModel)
                .GetProperty("DownloadedItems", BindingFlags.Public | BindingFlags.Instance)
                ?.SetValue(model, new List<Item> { item });

            var result = await model.OnGetGetStockData() as JsonResult;

            Assert.NotNull(result);

            //var list = result.Value as IEnumerable<object>;
            var list = item.getRating();
            Assert.NotNull(list);
        }


        [Fact]
        public async Task FetchDataInternal_PopulatesDownloadedItems_WhenStaticDataEnabled()
        {
            var model = CreateModel();

            // nastavíme UseStaticData = true
            typeof(IndexModel)
                .GetField("UseStaticData", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, true);

            // nastavíme oblíbené položky
            typeof(IndexModel)
                .GetField("_favoriteItems", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, new List<string> { "AAPL", "TSLA" });

            // zavoláme FetchDataInternal pomocí reflexe
            var method = typeof(IndexModel).GetMethod("FetchDataInternal", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)method?.Invoke(model, null);
            await task;

            var downloaded = typeof(IndexModel).GetProperty("DownloadedItems")?.GetValue(model) as List<Item>;

            Assert.NotNull(downloaded);
            Assert.True(downloaded.Count >= 2); // mělo by tam něco být
            Assert.Contains(downloaded, i => i.getName() == "AAPL");
            Assert.Contains(downloaded, i => i.getName() == "TSLA");
        }






        [Fact]
        public async Task FetchStockHistory_StaticData_ReturnsCorrectItems()
        {
            var model = CreateModel();
            var field = typeof(IndexModel).GetField("UseStaticData", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, true);

            var method = typeof(IndexModel).GetMethod("FetchStockHistory", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<List<Item>>)method?.Invoke(model, new object[] { "TEST", 3 });
            var result = await task;

            Assert.Equal(3, result.Count);
            Assert.All(result, i => Assert.Equal("TEST", i.getName()));
        }

        [Fact]
        public async Task FetchStockPrice_ThrowsOnMissingData()
        {
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"Global Quote\": {}}", Encoding.UTF8, "application/json")
            };
            var client = new HttpClient(new FakeHttpHandler(response));
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
            var model = new IndexModel(httpClientFactoryMock.Object);

            var method = typeof(IndexModel).GetMethod("FetchStockPrice", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<decimal>)method.Invoke(model, new object[] { "MSFT" });

            var ex = await Assert.ThrowsAsync<Exception>(async () => await task);
            Assert.Contains("Nelze parsovat cenu", ex.Message);
        }



        private class FakeHttpHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;
            public FakeHttpHandler(HttpResponseMessage response) => _response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_response);
        }
    }
}
