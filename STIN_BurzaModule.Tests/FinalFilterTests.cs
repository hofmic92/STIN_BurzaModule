using STIN_BurzaModule;
using STIN_BurzaModule.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace STIN_BurzaModule.Tests
{
    public class FinalFilterTests
    {
        [Fact]
        public void Filter_ReturnsOnlyLatestPerCompany()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var items = new List<Item>
            {
                new Item("A", now - 1000, 1),
                new Item("A", now - 500, 2),
                new Item("B", now - 3000, 1),
                new Item("B", now - 100, 2)
            };

            var filter = new FinalFilter();
            var result = filter.filter(items);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, i => i.getName() == "A" && i.getDate() == now - 500);
            Assert.Contains(result, i => i.getName() == "B" && i.getDate() == now - 100);
        }
    }
}