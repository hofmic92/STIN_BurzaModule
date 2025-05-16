using System.Text.Json.Serialization;

namespace STIN_BurzaModule.DataModel
{
    public class DataModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty; //pokus o vyřešení varování v githubu
        [JsonPropertyName("date")]
        public long Date { get; set; }
        [JsonPropertyName("rating")]
        public int Rating { get; set; }
        [JsonPropertyName("sell")]
        public int Sell { get; set; }
    }
}
