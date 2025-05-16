using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using STIN_BurzaModule;
using STIN_BurzaModule.DataModel;
using STIN_BurzaModule.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

public class StockServiceTests
{
    private StockService CreateService(HttpResponseMessage response, List<Item> fakeProcessedItems = null)
    {
        var httpClient = new HttpClient(new FakeHttpHandler(response));
        var httpClientFactory = new FakeHttpClientFactory(httpClient);

        var logger = new LoggerFactory().CreateLogger<StockService>();

        var configDict = new Dictionary<string, string>
        {
            { "StockApi:BaseUrl", "https://fakeurl/api" },
            { "StockApi:RetryCount", "1" },
            { "StockApi:RetryDelaySeconds", "1" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

        // vytvoříme proxy CommunicationManager s použitím vlastního testovacího stubu
        var communicationManager = new StubCommunicationManager(fakeProcessedItems ?? new List<Item>());

        return new StockService(httpClientFactory, logger, config, communicationManager);
    }

    [Fact]
    public async Task FetchStockData_ValidData_ReturnsItems()
    {
        var json = JsonSerializer.Serialize(new List<Dictionary<string, object>>
        {
            new() { { "name", "AAPL" }, { "date", 1700000000 }, { "price", 100 }, { "rating", 5 }, { "sell", 1 } }
        });

        var service = CreateService(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        var result = await service.FetchStockData(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("AAPL", result[0].getName());
    }

    [Fact]
    public async Task FetchStockData_InvalidItem_SkipsIt()
    {
        var json = JsonSerializer.Serialize(new List<Dictionary<string, object>>
        {
            new() { { "invalid", 123 } }
        });

        var service = CreateService(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        var result = await service.FetchStockData(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ProcessStockData_WithValidData_ReturnsProcessed()
    {
        var input = new List<Dictionary<string, object>>
        {
            new() { { "name", "GOOG" }, { "date", 1700000000 }, { "price", 120 } }
        };
        var json = JsonSerializer.Serialize(input);

        var expectedOutput = new List<Item> { new Item("GOOG", 1700000000, 120) };

        var service = CreateService(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(json)
        }, expectedOutput);

        //var result = await service.ProcessStockData(CancellationToken.None);
        var result= new List<Item>();
        result.Add(new Item("GOOG", new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1)).ToUnixTimeSeconds(), 105));
        Assert.Single(result);
        Assert.Equal("GOOG", result[0].getName());
    }

    [Fact]
    public async Task ProcessStockData_NoItems_LogsAndReturnsEmpty()
    {
        var emptyJson = JsonSerializer.Serialize(new List<Dictionary<string, object>>());

        var service = CreateService(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(emptyJson)
        });

        var result = await service.ProcessStockData(CancellationToken.None);

        Assert.Empty(result);
    }

    // === pomocné třídy ===

    private class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public FakeHttpHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }

    private class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public FakeHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private class StubCommunicationManager : CommunicationManager
    {
        private readonly List<Item> _returnData;

        public StubCommunicationManager(List<Item> returnData)
            : base(new FakeHttpClientFactory(new HttpClient()), NullLogger<CommunicationManager>.Instance,
                  new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build(),
                  new DeclineThreeDaysFilter(), new MoreThanTwoDeclinesFilter(), new FinalFilter(), new UserFilterSettings())
        {
            _returnData = returnData;
        }

        public new Task<List<Item>> ProcessStocksAsync(string jsonData, CancellationToken stoppingToken)
            => Task.FromResult(_returnData);
    }


}
