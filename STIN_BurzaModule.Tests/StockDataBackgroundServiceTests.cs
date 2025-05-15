using System.Collections.Generic;
using System.Threading.Tasks;
using STIN_BurzaModule;

namespace StockModule.Pages
{
    public class IndexModel
    {
        private List<Item> _items = new List<Item>();

        public async Task FetchDataInternal()
        {
            // Simulace načtení dat
            await Task.Delay(50); // simuluj async
            _items = new List<Item>
            {
                new Item("Test1", 1715700000, 100),
                new Item("Test2", 1715703600, 110)
            };
        }

        public List<Item> GetStockItems()
        {
            return _items;
        }

        // Případně další veřejné metody...
    }
}
