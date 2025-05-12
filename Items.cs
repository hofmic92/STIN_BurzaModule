using System.ComponentModel.DataAnnotations;

namespace STIN_BurzaModule
{
    public class Item
    {
        public string Name { get; set; }
        public long Date { get; set; } //UNIX timestamp
        public int? Rating { get; set; }
        public int? Sell { get; set; }
    }

}