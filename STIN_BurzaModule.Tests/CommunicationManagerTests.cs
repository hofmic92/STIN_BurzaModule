using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using STIN_BurzaModule;
using STIN_BurzaModule.Services;
using Xunit;

public class CommunicationManagerTests
{
    private static CommunicationManager CreateManager(string responseJson, int statusCode = 200)
    {
        var mockHttpFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<CommunicationManager>>();
        var mockConfig = new Mock<IConfiguration>();

        var settings = new UserFilterSettings
        {
            EnableDeclineThreeDays = false,
            EnableMoreThanTwoDeclines = false
        };

        var declineFilter = new DeclineThreeDaysFilter();
        var decline2Filter = new MoreThanTwoDeclinesFilter();
        var finalFilter = new FinalFilter();

        var message = new HttpResponseMessage
        {
            StatusCode = (HttpStatusCode)statusCode,
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };
        var client = new HttpClient(new FakeHttpHandler(message));
        mockHttpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        // ✅ Správné mockování přes GetSection().Value
        mockConfig.Setup(c => c["Communication:NewsUrl"]).Returns("http://fakeurl");
        mockConfig.Setup(c => c["Communication:Endpoints:ListStock"]).Returns("/liststock");
        mockConfig.Setup(c => c["Communication:Endpoints:SaleStock"]).Returns("/salestock");

        var sectionMock1 = new Mock<IConfigurationSection>();
        sectionMock1.Setup(s => s.Value).Returns("1");
        mockConfig.Setup(c => c.GetSection("Communication:Retry:MaxAttempts")).Returns(sectionMock1.Object);
        mockConfig.Setup(c => c.GetSection("Communication:Retry:DelaySeconds")).Returns(sectionMock1.Object);
        mockConfig.Setup(c => c.GetSection("Communication:TimeoutSeconds")).Returns(sectionMock1.Object);

        return new CommunicationManager(mockHttpFactory.Object, mockLogger.Object, mockConfig.Object,
            declineFilter, decline2Filter, finalFilter, settings);
    }


    [Fact]
    public async Task ProcessStocksAsync_ValidJson_ReturnsFinalItems()
    {
        var validJson = JsonSerializer.Serialize(new List<Dictionary<string, object>>
        {
            new() { { "name", "AAPL" }, { "date", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }, { "price", 123 } }
        });

        var manager = CreateManager(validJson);
        var result = await manager.ProcessStocksAsync(validJson, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("AAPL", result[0].getName());
    }

    [Fact]
    public async Task ProcessStocksAsync_EmptyJson_ReturnsEmptyList()
    {
        var manager = CreateManager("[]");
        var result = await manager.ProcessStocksAsync("[]", CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public void TryParseItem_InvalidItem_ReturnsFalse()
    {
        var manager = CreateManager("[]");
        var data = new Dictionary<string, object> { { "name", "" }, { "date", "abc" } };

        var method = typeof(CommunicationManager).GetMethod("TryParseItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var parameters = new object[] { data, null };
        var success = (bool)method.Invoke(manager, parameters);

        Assert.False(success);
    }

    [Fact]
    public void DeserializeJson_InvalidJson_ReturnsEmptyList()
    {
        var manager = CreateManager("[]");

        var method = typeof(CommunicationManager).GetMethod("DeserializeJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (List<Item>)method.Invoke(manager, new object[] { "INVALID_JSON" });

        Assert.Empty(result);
    }

    private class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public FakeHttpHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }
}
