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
    public void Firm_WithTwoOrFewerDeclines_IsKept()
    {
        var filter = new MoreThanTwoDeclinesFilter();
        var items = new List<Item>
        {
            new Item("SafeCorp", UnixDaysAgo(5), 120),
            new Item("SafeCorp", UnixDaysAgo(4), 110), // pokles
            new Item("SafeCorp", UnixDaysAgo(3), 115), // růst
            new Item("SafeCorp", UnixDaysAgo(2), 100), // pokles
            new Item("SafeCorp", UnixDaysAgo(1), 105), // růst
        };

        var result = filter.filter(items);

        Assert.Contains(result, i => i.getName() == "SafeCorp");
    }

    [Fact]
    public void Firm_WithMoreThanTwoDeclines_IsFilteredOut()
    {
        var filter = new MoreThanTwoDeclinesFilter();
        var items = new List<Item>
    {
        new Item("RiskCorp", UnixDate("2024-05-15"), 200), // středa
        new Item("RiskCorp", UnixDate("2024-05-16"), 190), // čtvrtek (↓)
        new Item("RiskCorp", UnixDate("2024-05-17"), 180), // pátek (↓)
        new Item("RiskCorp", UnixDate("2024-05-20"), 170), // pondělí (↓)
        new Item("RiskCorp", UnixDate("2024-05-21"), 160), // úterý (↓)
    };

        var result = filter.filter(items);

        Assert.DoesNotContain(result, i => i.getName() == "RiskCorp");
    }

    private long UnixDate(string dateStr)
    {
        var dt = DateTime.SpecifyKind(DateTime.Parse(dateStr), DateTimeKind.Utc);
        return ((DateTimeOffset)dt).ToUnixTimeSeconds();
    }


    [Fact]
    public void Filter_IgnoresWeekendDates()
    {
        var filter = new MoreThanTwoDeclinesFilter();

        // sobota a neděle (simulujeme víkend)
        DateTime saturday = new DateTime(2024, 12, 28); // Sobota
        DateTime sunday = new DateTime(2024, 12, 29);   // Neděle
        DateTime monday = new DateTime(2024, 12, 30);   // Pondělí

        var items = new List<Item>
        {
            new Item("WeekendCorp", new DateTimeOffset(saturday).ToUnixTimeSeconds(), 100),
            new Item("WeekendCorp", new DateTimeOffset(sunday).ToUnixTimeSeconds(), 90),
            new Item("WeekendCorp", new DateTimeOffset(monday).ToUnixTimeSeconds(), 80),
        };

        var result = filter.filter(items);

        // Firma má pouze 1 pracovní den → < 5 záznamů → nesmí být filtrována
        Assert.Contains(result, i => i.getName() == "WeekendCorp");
    }

    [Fact]
    public void UnixTimeToDateTime_ConvertsCorrectly()
    {
        var filter = new MoreThanTwoDeclinesFilter();
        var dt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var unix = new DateTimeOffset(dt).ToUnixTimeSeconds();

        var result = InvokeUnixToDateTime(filter, unix);

        Assert.Equal(dt.Date, result.Date);
    }

    private DateTime InvokeUnixToDateTime(object filter, long unixTime)
    {
        var method = filter.GetType().GetMethod("UnixTimeToDateTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (DateTime)method.Invoke(filter, new object[] { unixTime });
    }

    [Theory]
    [InlineData("2024-12-28", false)] // Sobota
    [InlineData("2024-12-29", false)] // Neděle
    [InlineData("2024-12-30", true)]  // Pondělí
    public void IsWorkingDay_ReturnsCorrectResult(string dateString, bool expected)
    {
        var filter = new MoreThanTwoDeclinesFilter();
        var date = DateTime.Parse(dateString);

        var result = InvokeIsWorkingDay(filter, date);

        Assert.Equal(expected, result);
    }

    private bool InvokeIsWorkingDay(object filter, DateTime date)
    {
        var method = filter.GetType().GetMethod("IsWorkingDay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (bool)method.Invoke(filter, new object[] { date });
    }
}
