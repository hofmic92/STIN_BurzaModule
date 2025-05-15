using STIN_BurzaModule;
using STIN_BurzaModule.Filters;
using System;
using System.Collections.Generic;
using Xunit;

namespace STIN_BurzaModule.Tests
{
    public class DeclineThreeDaysFilterTests
    {
        [Fact]
        public void Filter_RemovesFirmWithThreeConsecutiveDeclines()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var items = new List<Item>
            {
                new Item("Apple", now - 86400 * 3, 0) { Price = 105 },
                new Item("Apple", now - 86400 * 2, 0) { Price = 102 },
                new Item("Apple", now - 86400 * 1, 0) { Price = 100 },
                new Item("Google", now - 86400 * 3, 0) { Price = 200 },
                new Item("Google", now - 86400 * 2, 0) { Price = 205 },
                new Item("Google", now - 86400 * 1, 0) { Price = 210 }
            };

            var filter = new DeclineThreeDaysFilter();
            var result = filter.filter(items);

            Assert.DoesNotContain(result, i => i.getName() == "Apple");
            Assert.Contains(result, i => i.getName() == "Google");
        }
    }
}