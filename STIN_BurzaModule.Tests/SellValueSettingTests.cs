using Microsoft.Extensions.Configuration;
using STIN_BurzaModule.ConfigClasses;
using Xunit;

public class SellValueSettingTests
{
    [Fact]
    public void CanBindSellValueFromConfiguration()
    {
        // simulujeme konfiguraci jako by byla v appsettings.json
        var settings = new Dictionary<string, string>
        {
            { "SellValue:SellOrNo", "1" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var bound = config.GetSection("SellValue").Get<SellValueSetting>();

        Assert.NotNull(bound);
        Assert.Equal(1, bound.SellOrNo);
    }
}
