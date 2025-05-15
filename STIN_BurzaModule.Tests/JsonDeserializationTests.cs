using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace STIN_BurzaModule.Tests
{
    public class JsonDeserializationTests
    {
        [Fact]
        public void CanDeserializeListOfDataModel()
        {
            var json = @"[
                { \"name\": \"TestCorp\", \"date\": 1715700000, \"rating\": 5, \"sell\": 1 },
                { \"name\": \"SecondCorp\", \"date\": 1715703600, \"rating\": 8, \"sell\": 0 }
            ]";

            var result = JsonSerializer.Deserialize<List<DataModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("TestCorp", result[0].Name);
            Assert.Equal(1, result[0].Sell);
        }
    }
}