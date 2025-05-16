using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using STIN_BurzaModule;
using STIN_BurzaModule.Services;
using Xunit;

public class StockServiceTests
{
    [Fact]
    public async Task FetchStockData_ReturnsValidItems()
    {
        // Nastavíme mock HttpMessageHandler, který simuluje odpovìï API
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{\"name\":\"Test\",\"date\":1715700000,\"price\":100}]")
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);

        // Spoleèné mock factory pro StockService i CommunicationManager
        var clientFactoryMock = new Mock<IHttpClientFactory>();
        clientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Prázdná konfigurace
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        // Logger
        var logger = Mock.Of<ILogger<StockService>>();

        // Komunikaèní manager s platným klientem
        var commManager = new CommunicationManager(
            httpClientFactory: clientFactoryMock.Object,
            logger: new Mock<ILogger<CommunicationManager>>().Object,
            config: config,
            declineThreeDaysFilter: new DeclineThreeDaysFilter(),
            moreThanTwoDeclinesFilter: new MoreThanTwoDeclinesFilter(),
            finalFilter: new FinalFilter(),
            userFilterSettings: new UserFilterSettings()
        );

        // Testovaná služba
        var service = new StockService(clientFactoryMock.Object, logger, config, commManager);
        var result = await service.FetchStockData(CancellationToken.None);

        // Ovìøení výstupu
        Assert.Single(result);
        Assert.Equal("Test", result[0].getName());
    }
}
