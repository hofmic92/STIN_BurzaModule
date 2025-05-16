using Microsoft.Extensions.Configuration;
using STIN_BurzaModule;
using Xunit;

public class UserFilterSettingsTests
{
    [Fact]
    public void CanBindFromConfiguration()
    {
        var dict = new Dictionary<string, string>
        {
            { "UserFilters:EnableDeclineThreeDays", "true" },
            { "UserFilters:EnableMoreThanTwoDeclines", "false" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();

        var settings = config.GetSection("UserFilters").Get<UserFilterSettings>();

        Assert.True(settings.EnableDeclineThreeDays);
        Assert.False(settings.EnableMoreThanTwoDeclines);
    }
}
