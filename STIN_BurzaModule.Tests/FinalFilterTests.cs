using STIN_BurzaModule;
using STIN_BurzaModule.Filters;
using System;
using System.Collections.Generic;
using Xunit;

public class FinalFilterTests
{
    private long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Fact]
    public void KeepsOnlyMostRecentPerFirm()
    {
        var filter = new FinalFilter();
        var items = new List<Item>
        {
            new Item("A", Now - 1000, 10),
            new Item("A", Now - 500, 11),
            new Item("B", Now - 2000, 8),
            new Item("B", Now - 100, 9)
        };

        var result = filter.filter(items);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.getName() == "A" && i.getDate() == Now - 500);
        Assert.Contains(result, i => i.getName() == "B" && i.getDate() == Now - 100);
    }
}
