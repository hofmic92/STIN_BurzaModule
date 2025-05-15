using STIN_BurzaModule;
using STIN_BurzaModule.Filters;
using System;
using System.Collections.Generic;
using Xunit;

namespace STIN_BurzaModule.Tests
{
    public class MoreThanTwoDeclinesFilterTests
    {
        [Fact]
        public void Filter_RemovesFirmWithMoreThanTwoDeclines()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var items = new List<Item>
            {
                new Item("FirmX", now - 86400 * 5, 0) { Price = 120 },
                new Item("FirmX", now - 86400 * 4, 0) { Price = 115 },
                new Item("FirmX", now - 86400 * 3, 0) { Price = 110 },
                new Item("FirmX", now - 86400 * 2, 0) { Price = 100 },
                new Item("FirmX", now - 86400 * 1, 0) { Price = 90 },

                new Item("FirmY", now - 86400 * 3, 0) { Price = 100 },
                new Item("FirmY", now - 86400 * 2, 0) { Price = 105 },
                new Item("FirmY", now - 86400 * 1, 0) { Price = 110 }
            };

            var filter = new MoreThanTwoDeclinesFilter();
            var result = filter.filter(items);

            Assert.DoesNotContain(result, i => i.getName() == "FirmX");
            Assert.Contains(result, i => i.getName() == "FirmY");
        }
    }
}