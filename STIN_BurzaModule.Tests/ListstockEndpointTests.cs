using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using STIN_BurzaModule;
using STIN_BurzaModule.DataModel;
using STIN_BurzaModule.Pages;
using Xunit;
using System.Collections.Generic;
using StockModule.Pages;

public class ListstockEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ListstockEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Vytvoření vlastní instance IndexModel a naplnění dat
                var testModel = new IndexModel();
                typeof(IndexModel)
                    .GetProperty("DownloadedItems")!
                    .SetValue(testModel, new List<Item>
                    {
                        new Item("Apple", 1715700000, 100),
                        new Item("Google", 1715703600, 90)
                    });

                services.AddSingleton<IndexModel>(testModel);
            });
        }).CreateClient();
        Assert.True(true);
    }

    
}
