using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using STIN_BurzaModule;
using STIN_BurzaModule.Filters;
using STIN_BurzaModule.Services;
using Xunit;

public class CommunicationManagerTests
{
    [Fact]
    public async void ProcessStocksAsync_ReturnsFilteredList()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "Communication:NewsUrl", "http://test" },
            { "Communication:Endpoints:ListStock", "/liststock" },
            { "Communication:Endpoints:SaleStock", "/salestock" },
        }).Build();

        var logger = Mock.Of<ILogger<CommunicationManager>>();
        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(new HttpClient(new Mock<HttpMessageHandler>().Object));

        var decline = new DeclineThreeDaysFilter();
        var more = new MoreThanTwoDeclinesFilter();
        var final = new FinalFilter();

        var settings = new UserFilterSettings { EnableDeclineThreeDays = false, EnableMoreThanTwoDeclines = false };

        var comm = new CommunicationManager(clientFactory.Object, logger, config, decline, more, final, settings);

        string json = JsonSerializer.Serialize(new List<Item> { new Item("Test", 1715700000, 100) });
        var result = await comm.ProcessStocksAsync(json, new CancellationToken());

        Assert.NotNull(result);
    }
}