using Xunit;
using System;
using STIN_BurzaModule;

namespace STIN_BurzaModule.Tests
{
    public class ItemTests
    {
        [Fact]
        public void Constructor_WithPrice_SetsPropertiesCorrectly()
        {
            var item = new Item("TestItem", 1700000000, 100);

            Assert.Equal("TestItem", item.getName());
            Assert.Equal(1700000000, item.getDate());
            Assert.Equal(100, item.getPrice());
            Assert.Equal(0, item.getRating());
            Assert.Equal(0, item.getSell());
        }

        [Fact]
        public void Constructor_WithoutPrice_SetsDefaultPriceToZero()
        {
            var item = new Item("TestItem", 1700000000);

            Assert.Equal(0, item.getPrice());
        }

        [Fact]
        public void SetRating_ValidValue_UpdatesRating()
        {
            var item = new Item("TestItem", 1700000000);
            item.setRating(5);

            Assert.Equal(5, item.getRating());
        }

        [Fact]
        public void SetRating_InvalidHigh_ThrowsException()
        {
            var item = new Item("TestItem", 1700000000);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => item.setRating(15));
            Assert.Contains("Rating must be between", ex.Message);
        }

        [Fact]
        public void SetSell_RatingBelowSellValue_SetsSellToOne()
        {
            var item = new Item("TestItem", 1700000000);
            item.setRating(0); // default sellvalue is 1
            item.setSell();

            Assert.Equal(1, item.getSell());
        }

        [Fact]
        public void SetSell_RatingAboveOrEqualSellValue_SetsSellToZero()
        {
            var item = new Item("TestItem", 1700000000);
            item.setRating(2); // default sellvalue is 1
            item.setSell();

            Assert.Equal(0, item.getSell());
        }

        [Fact]
        public void CanChangeMinAndMaxRatingAndSetRating()
        {
            var item = new Item("TestItem", 1700000000);
            item.setMinrating(-5);
            item.setMaxrating(5);
            item.setRating(4);

            Assert.Equal(4, item.getRating());
        }

        [Fact]
        public void CanChangeSellValue()
        {
            var item = new Item("TestItem", 1700000000);
            item.setSellValue(3);
            item.setRating(2);
            item.setSell();

            Assert.Equal(1, item.getSell());
        }
    }
    public class ItemTests_Extended
    {
        [Theory]
        [InlineData(-10)]
        [InlineData(10)]
        public void SetRating_AtExactLimits_ThrowsException(int input)
        {
            var item = new Item("TestItem", 1700000000);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => item.setRating(input));
            Assert.Contains("Rating must be between", ex.Message);
        }

        [Theory]
        [InlineData(-9)]
        [InlineData(0)]
        [InlineData(9)]
        public void SetRating_WithinValidRange_Succeeds(int input)
        {
            var item = new Item("TestItem", 1700000000);
            item.setRating(input);

            Assert.Equal(input, item.getRating());
        }

        [Theory]
        [InlineData(-11)]
        [InlineData(11)]
        public void SetRating_OutOfRange_Throws(int input)
        {
            var item = new Item("TestItem", 1700000000);

            Assert.Throws<ArgumentOutOfRangeException>(() => item.setRating(input));
        }
    }

}
