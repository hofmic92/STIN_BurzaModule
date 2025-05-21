using STIN_BurzaModule;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class DeclineThreeDaysFilterTests
{
    private long UnixDaysAgo(int days) =>
        new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-days)).ToUnixTimeSeconds();

    [Fact]
    public void FirmWithLessThanThreeWorkingDays_IsNotFiltered()
    {
        var filter = new DeclineThreeDaysFilter();
        var items = new List<Item>
        {
            new Item("ShortCorp", UnixDaysAgo(1), 100),
            new Item("ShortCorp", UnixDaysAgo(2), 102) // pouze 2 dny
        };

        var result = filter.filter(items);

        Assert.Contains(result, i => i.getName() == "ShortCorp");
    }

    [Fact]
    public void FirmWithThreeDecliningWorkingDays_IsFilteredOut()
    {
        var filter = new DeclineThreeDaysFilter();
        var items = new List<Item>
        {
            new Item("Decliner", UnixDaysAgo(3), 105),
            new Item("Decliner", UnixDaysAgo(2), 102),
            new Item("Decliner", UnixDaysAgo(1), 100)
        };

        var result = filter.filter(items);

        Assert.DoesNotContain(result, i => i.getName() == "Decliner");
    }

    [Fact]
    public void FirmWithStableOrRisingPrices_IsNotFiltered()
    {
        var filter = new DeclineThreeDaysFilter();
        var items = new List<Item>
        {
            new Item("Riser", UnixDaysAgo(3), 100),
            new Item("Riser", UnixDaysAgo(2), 105),
            new Item("Riser", UnixDaysAgo(1), 110)
        };

        var result = filter.filter(items);

        Assert.Contains(result, i => i.getName() == "Riser");
    }

    [Fact]
    public void FirmWithMixedChanges_IsNotFiltered()
    {
        var filter = new DeclineThreeDaysFilter();
        var items = new List<Item>
        {
            new Item("Mixed", UnixDaysAgo(3), 105),
            new Item("Mixed", UnixDaysAgo(2), 100),
            new Item("Mixed", UnixDaysAgo(1), 102) // 100 → 102 = růst
        };

        var result = filter.filter(items);

        Assert.Contains(result, i => i.getName() == "Mixed");
    }
}