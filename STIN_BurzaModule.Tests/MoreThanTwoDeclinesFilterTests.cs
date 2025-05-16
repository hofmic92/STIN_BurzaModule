using STIN_BurzaModule;
using STIN_BurzaModule.Filters;
using System;
using System.Collections.Generic;
using Xunit;

public class MoreThanTwoDeclinesFilterTests
{
    private long UnixDaysAgo(int days) =>
        new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-days)).ToUnixTimeSeconds();

    [Fact]
    public void FirmWithMoreThanTwoDeclines_IsFilteredOut()
    {
        var filter = new MoreThanTwoDeclinesFilter();

        var prices = new[] { 140, 130, 120, 110, 100, 95, 90, 85 }; // 6 poklesů
        var items = new List<Item>();

        for (int i = 0; i < prices.Length; i++)
        {
            items.Add(new Item("Apple", UnixDaysAgo(prices.Length - i), prices[i]));
        }

        var result = filter.filter(items);

        Assert.DoesNotContain(result, i => i.getName() == "Apple");
    }

    [Fact]
    public void FirmWithMaxTwoDeclines_IsNotFiltered()
    {
        var filter = new MoreThanTwoDeclinesFilter();

        var prices = new[] { 100, 101, 99, 102, 103, 104, 105, 106 }; // 2 poklesy
        var items = new List<Item>();

        for (int i = 0; i < prices.Length; i++)
        {
            items.Add(new Item("Google", UnixDaysAgo(prices.Length - i), prices[i]));
        }

        var result = filter.filter(items);

        Assert.Contains(result, i => i.getName() == "Google");
    }
}
