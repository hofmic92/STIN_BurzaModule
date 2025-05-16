using System.Text.Json.Serialization;

namespace STIN_BurzaModule.DataModel
{
    public class DataModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("date")]
        public long Date { get; set; }
        [JsonPropertyName("rating")]
        public int Rating { get; set; }
        [JsonPropertyName("sell")]
        public int Sell { get; set; }
    }
}
