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
        // Předpokládáme, že dnes je např. úterý → pracovní dny jsou: pátek, pondělí, úterý
        // Vytvoříme data pro pátek, pondělí a úterý s klesající cenou
        new Item("Decliner", UnixDate("2024-05-17"), 105), // pátek
        new Item("Decliner", UnixDate("2024-05-20"), 102), // pondělí
        new Item("Decliner", UnixDate("2024-05-21"), 100), // úterý
    };

        var result = filter.filter(items);

        Assert.DoesNotContain(result, i => i.getName() == "Decliner");
    }

    // Pomocná metoda pro převod data na unix timestamp
    private long UnixDate(string dateStr)
    {
        var dt = DateTime.SpecifyKind(DateTime.Parse(dateStr), DateTimeKind.Utc);
        return ((DateTimeOffset)dt).ToUnixTimeSeconds();
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