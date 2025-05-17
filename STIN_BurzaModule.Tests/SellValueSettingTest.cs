using STIN_BurzaModule.ConfigClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class SellValueSettingTests
{
    [Fact]
    public void SellValueSetting_CanSetAndGetSellOrNo()
    {
        // Arrange
        var setting = new SellValueSetting();
        int testValue = 1;

        // Act
        setting.SellOrNo = testValue;
        var result = setting.SellOrNo;

        // Assert
        Assert.Equal(testValue, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void SellValueSetting_CanHandleVariousValues(int testValue)
    {
        // Arrange
        var setting = new SellValueSetting();

        // Act
        setting.SellOrNo = testValue;

        // Assert
        Assert.Equal(testValue, setting.SellOrNo);
    }

    [Fact]
    public void SellValueSetting_DefaultValueIsZero()
    {
        // Arrange & Act
        var setting = new SellValueSetting();

        // Assert
        Assert.Equal(0, setting.SellOrNo);
    }
}
