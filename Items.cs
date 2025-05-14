using System.ComponentModel.DataAnnotations;

namespace STIN_BurzaModule
{
    public class Item
    {
        private int price;
        private int maxrating = 10;
        private int minrating = -10;
        private int sellvalue = 1;
        private string Name { get; set; }
        private long Date { get; set; } //UNIX timestamp
        private int? Rating { get; set; }
        private int? Sell { get; set; }

        //gettery
        public string getName()
        {
            return Name;
        }
        public long getDate()
        {
            return Date;
        }
        public int? getRating()
        {
            return Rating;
        }
        public int? getSell()
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
        }
        //settery
        public void setSell()
        {
            if (Sell >= sellvalue)
            {
                Sell = 1;
            }
            else
            {
                Sell = 0;
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
            this.Sell = value;
        }
    }



}