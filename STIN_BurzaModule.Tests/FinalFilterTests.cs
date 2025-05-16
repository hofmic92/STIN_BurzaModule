using STIN_BurzaModule;
using STIN_BurzaModule.Filters;
using System;
using System.Collections.Generic;
using Xunit;

public class FinalFilterTests
{
    private long UnixDaysAgo(int days) =>
        new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-days)).ToUnixTimeSeconds();

    [Fact]
    public void Filter_ReturnsOnlyLatestItemPerCompany()
    {
        var filter = new FinalFilter();
        var items = new List<Item>
        {
            new Item("A", UnixDaysAgo(3), 100),
            new Item("A", UnixDaysAgo(2), 120),
            new Item("A", UnixDaysAgo(1), 150), // nejnovější
            new Item("B", UnixDaysAgo(5), 90),
            new Item("B", UnixDaysAgo(2), 95),  // nejnovější
            new Item("C", UnixDaysAgo(0), 130)  // jediný → musí zůstat
        };

        var result = filter.filter(items);

        Assert.Equal(3, result.Count); // 3 firmy → 3 výsledky

        var a = result.Find(i => i.getName() == "A");
        var b = result.Find(i => i.getName() == "B");
        var c = result.Find(i => i.getName() == "C");

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotNull(c);

        Assert.Equal(150, a.getPrice()); // A → nejnovější
        Assert.Equal(95, b.getPrice());  // B → nejnovější
        Assert.Equal(130, c.getPrice()); // C → jediný záznam
    }
}
