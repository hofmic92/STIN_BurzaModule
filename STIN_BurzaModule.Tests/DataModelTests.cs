using System.Text.Json;
using STIN_BurzaModule.DataModel;
using Xunit;

public class DataModelTests
{
    [Fact]
    public void CanDeserializeFromJson()
    {
        string json = @"{
            ""name"": ""Microsoft"",
            ""date"": 1715700000,
            ""rating"": 8,
            ""sell"": 1
        }";

        var model = JsonSerializer.Deserialize<DataModel>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(model);
        Assert.Equal("Microsoft", model.Name);
        Assert.Equal(1715700000, model.Date);
        Assert.Equal(8, model.Rating);
        Assert.Equal(1, model.Sell);
    }

    [Fact]
    public void CanSerializeToJson()
    {
        var model = new DataModel
        {
            Name = "Google",
            Date = 1715703600,
            Rating = 10,
            Sell = 0
        };

        var json = JsonSerializer.Serialize(model);

        Assert.Contains(@"""name"":""Google""", json);
        Assert.Contains(@"""date"":1715703600", json);
        Assert.Contains(@"""rating"":10", json);
        Assert.Contains(@"""sell"":0", json);
    }
}
