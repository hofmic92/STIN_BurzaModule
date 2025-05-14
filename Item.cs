using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace STIN_BurzaModule
{
    public class Item
    {
        private int price;
        private int maxrating = 10;
        private int minrating = -10;
        private int sellvalue = 1;
        [JsonPropertyName("name")]
        private string Name { get; set; }
        [JsonPropertyName("date")]
        private long Date { get; set; } //UNIX timestamp
        [JsonPropertyName("rating")]
        private int Rating { get; set; }
        [JsonPropertyName("sell")]
        private int Sell { get; set; }

        //gettery
        public string getName()
        {
            return Name;
        }
        public long getDate()
        {
            return Date;
        }
        public int getRating()
        {
            return Rating;
        }
        public int getSell()
        {
            return Sell;
        }
        public int getPrice()
        {
            return price;
        }


        public Item(string name, long date, int price)
        {
            this.Name = name;
            this.Date = date;
            this.price = price;
            this.Rating = 0;
            this.Sell = 0;
        }

        public Item(string name, long date)
        {
            this.Name = name;
            this.Date = date;
            this.price = 0;
            this.Rating = 0;
            this.Sell = 0;
        }
        //settery
        public void setSell()
        {
            if (this.Rating >= sellvalue)
            {
                Sell = 0;
            }
            else
            {
                Sell = 1;
            }
        }
        public void setRating(int rating)
        {
            if (rating > minrating && rating < maxrating)
            {
                this.Rating = rating;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(rating),
            $"Rating must be between {minrating} and {maxrating}. Given: {rating}");
            }
        }

        public void setMaxrating(int maxrating)
        {
            this.maxrating = maxrating;
        }
        public void setMinrating(int minrating)
        {
            this.minrating = minrating;
        }
        public void setSellValue(int value)
        {
            this.sellvalue = value;
        }
    }



}