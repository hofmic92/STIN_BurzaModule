using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using STIN_BurzaModule;
using Xunit;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System.IO;

namespace STIN_BurzaModule.Tests
{
    public class SellForwarderTests
    {
        [Fact]
        public async Task ReceiveAndForwardToSaleEndpoint_Works()
        {
            // Arrange
            var sampleItems = new List<DataModel>
            {
                new DataModel { Name = "Test", Date = 1715700000, Rating = 7, Sell = 0 }
            };

            var json = JsonSerializer.Serialize(sampleItems);
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var request = new DefaultHttpContext().Request;
            request.Body = requestBody;

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });

            var httpClient = new HttpClient(handlerMock.Object);

            var inMemorySettings = new Dictionary<string, string> {
                { "NewsModule:SaleStockEndpoint", "https://localhost:8000/salestock" }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var logger = Mock.Of<ILogger<SellForwarder>>();

            var forwarder = new SellForwarder(httpClient, configuration, logger);

            // Act & Assert
            await forwarder.ReceiveAndForwardToSaleEndpoint(request);
        }
    }
}