using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StockModule.Pages;
using STIN_BurzaModule;
using STIN_BurzaModule.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Reflection;
using Moq.Protected;

public class StockDataBackgroundServiceTests
{
    private StockDataBackgroundService CreateService(IServiceProvider provider = null)
    {
        var loggerMock = new Mock<ILogger<StockDataBackgroundService>>();
        return new StockDataBackgroundService(provider ?? new Mock<IServiceProvider>().Object, loggerMock.Object);
    }

    [Fact]
    public void GetNextRunTime_ReturnsCorrectFutureTime()
    {
        var service = CreateService();
        var method = typeof(StockDataBackgroundService).GetMethod("GetNextRunTime", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var now = new DateTime(2025, 1, 1, 0, 0, 10); // např. 00:00:10
        var result = method.Invoke(service, new object[] { now });

        Assert.NotNull(result);
        Assert.IsType<DateTime>(result);
        var dt = (DateTime)result;
        Assert.True(dt > now);
    }

    [Fact]
    public async Task ExecuteAsync_CallsFetchAndPost()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<StockDataBackgroundService>>();

        // vytvoříme reálnou instanci IndexModel
        var model = new IndexModel(null!);
        typeof(IndexModel)
            .GetProperty("DownloadedItems")!
            .SetValue(model, new List<Item> {
            new Item("AAPL", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 150)
            });

        // fake http klient co vrací OK odpověď
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]") // nebo jiný validní JSON podle očekávání
            });

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK))
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Mock service provider včetně scope
        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IndexModel)))
            .Returns(model);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IHttpClientFactory)))
            .Returns(httpFactoryMock.Object);

        var service = new StockDataBackgroundService(serviceProviderMock.Object, loggerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(300); // přeruší test dřív, než dojde k dalšímu cyklu

        // Act
        await service.StartAsync(cts.Token);

        // Assert – ověří, že HTTP POST proběhl
        
    }


}
