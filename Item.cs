using System.ComponentModel.DataAnnotations;

namespace STIN_BurzaModule
{
    public class Item
    {
        [Required]
        public required string Name { get; set; } // Používá required místo nullability

        [Required]
        public long Date { get; set; } // UNIX timestamp

        [Range(-10, 10, ErrorMessage = "Rating must be between -10 and 10.")]
        public int? Rating { get; set; }

        [Range(0, 1, ErrorMessage = "Sell must be 0 or 1.")]
        public int? Sell { get; set; }
    }
}