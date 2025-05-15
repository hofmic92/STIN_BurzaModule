using STIN_BurzaModule;
using System;
using Xunit;

namespace STIN_BurzaModule.Tests
{
    public class ItemTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var item = new Item("Microsoft", 1715700000, 5);
            Assert.Equal("Microsoft", item.getName());
            Assert.Equal(1715700000, item.getDate());
            Assert.Equal(5, item.getRating());
        }

        [Fact]
        public void SetSell_SetsSellToOne()
        {
            var item = new Item("Test", 0, 0);
            item.setSell();
            Assert.Equal(1, item.getSell());
        }
    }
}