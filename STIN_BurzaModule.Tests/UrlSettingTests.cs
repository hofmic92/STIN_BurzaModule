using Microsoft.Extensions.Configuration;
using STIN_BurzaModule.ConfigClasses;
using Xunit;

public class UrlSettingTests
{
    [Fact]
    public void CanBindEnableUrlFromConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            { "Urls:EnableUrl", "https://localhost:8000/salestock" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var urlSetting = config.GetSection("Urls").Get<UrlSetting>();

        Assert.NotNull(urlSetting);
        Assert.Equal("https://localhost:8000/salestock", urlSetting.EnableUrl);
    }
}
