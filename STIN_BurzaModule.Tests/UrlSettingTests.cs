using STIN_BurzaModule.ConfigClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class UrlSettingTests
{
    [Fact]
    public void UrlSetting_CanSetAndGetEnableUrl()
    {
        // Arrange
        var setting = new UrlSetting();
        string testUrl = "https://example.com/api/enable";

        // Act
        setting.EnableUrl = testUrl;
        var result = setting.EnableUrl;

        // Assert
        Assert.Equal(testUrl, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:8080")]
    [InlineData("just a string")]
    public void UrlSetting_CanHandleVariousUrlValues(string testUrl)
    {
        // Arrange
        var setting = new UrlSetting();

        // Act
        setting.EnableUrl = testUrl;

        // Assert
        Assert.Equal(testUrl, setting.EnableUrl);
    }

    [Fact]
    public void UrlSetting_DefaultValueIsNull()
    {
        // Arrange & Act
        var setting = new UrlSetting();

        // Assert
        Assert.Null(setting.EnableUrl);
    }

    [Fact]
    public void UrlSetting_CanHandleLongUrls()
    {
        // Arrange
        var setting = new UrlSetting();
        var longUrl = new string('a', 10000); // Very long URL

        // Act
        setting.EnableUrl = longUrl;

        // Assert
        Assert.Equal(longUrl, setting.EnableUrl);
    }
}
