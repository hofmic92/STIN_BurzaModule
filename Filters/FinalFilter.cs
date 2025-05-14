using STIN_BurzaModule.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STIN_BurzaModule
{
    public class FinalFilter : Filter
    {
        public override List<Item> filter(List<Item> items)
        {
            return items
                .GroupBy(i => i.getName())
                .Select(g => g.OrderByDescending(i => i.getDate()).First())
                .ToList();
        }
    }
}
