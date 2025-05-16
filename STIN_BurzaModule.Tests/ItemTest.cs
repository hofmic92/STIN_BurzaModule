using System;
using STIN_BurzaModule;
using Xunit;

public class ItemTests
{
    [Fact]
    public void Constructor_WithPrice_SetsProperties()
    {
        var item = new Item("Apple", 1715700000, 150);
        Assert.Equal("Apple", item.getName());
        Assert.Equal(1715700000, item.getDate());
        Assert.Equal(150, item.getPrice());
        Assert.Equal(0, item.getRating());
        Assert.Equal(0, item.getSell());
    }

    [Fact]
    public void Constructor_WithoutPrice_SetsDefaults()
    {
        var item = new Item("Google", 1715700001);
        Assert.Equal("Google", item.getName());
        Assert.Equal(1715700001, item.getDate());
        Assert.Equal(0, item.getPrice());
        Assert.Equal(0, item.getRating());
        Assert.Equal(0, item.getSell());
    }

    [Theory]
    [InlineData(-9)]
    [InlineData(0)]
    [InlineData(9)]
    public void SetRating_ValidValues_AreAccepted(int rating)
    {
        var item = new Item("Test", 1715700000, 0);
        item.setRating(rating);
        Assert.Equal(rating, item.getRating());
    }

    [Theory]
    [InlineData(-11)]
    [InlineData(10)]
    [InlineData(100)]
    public void SetRating_InvalidValues_ThrowsException(int rating)
    {
        var item = new Item("Test", 1715700000, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => item.setRating(rating));
    }

    [Theory]
    [InlineData(5, 6, 1)] // Rating < SellValue => Sell = 1
    [InlineData(7, 6, 0)] // Rating >= SellValue => Sell = 0
    [InlineData(6, 6, 0)] // Rating == SellValue => Sell = 0
    public void SetSell_BehavesAsExpected(int rating, int sellValue, int expectedSell)
    {
        var item = new Item("Test", 1715700000, 0);
        item.setRating(rating);
        item.setSellValue(sellValue);
        item.setSell();
        Assert.Equal(expectedSell, item.getSell());
    }

    [Fact]
    public void SetMaxAndMinRating_AllowsCustomRange()
    {
        var item = new Item("CustomRange", 1715700000, 0);
        item.setMaxrating(20);
        item.setMinrating(-20);
        item.setRating(15);
        Assert.Equal(15, item.getRating());
    }

    [Fact]
    public void SetMaxAndMinRating_ThrowsOutsideNewRange()
    {
        var item = new Item("RangeFail", 1715700000, 0);
        item.setMaxrating(5);
        item.setMinrating(-5);

        Assert.Throws<ArgumentOutOfRangeException>(() => item.setRating(11));
        Assert.Throws<ArgumentOutOfRangeException>(() => item.setRating(-11));
    }
}
