using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using STIN_BurzaModule.DataModel;
using Xunit;

// Nezapomeň přidat: public partial class Program {} na konec Program.cs

public class SalestockEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SalestockEndpointTests(WebApplicationFactory<Program> factory)

    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_SaleStock_ReturnsOkWithUpdatedSellValues()
    {
        var input = new List<DataModel>
        {
            new DataModel { Name = "OpenAI", Date = 1715700000, Rating = -5 },
            new DataModel { Name = "Google", Date = 1715703600, Rating = 5 }
        };

        var content = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/salestock", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var resultJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<DataModel>>(resultJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.All(result, item => Assert.InRange(item.Sell, 0, 1));
    }
}
