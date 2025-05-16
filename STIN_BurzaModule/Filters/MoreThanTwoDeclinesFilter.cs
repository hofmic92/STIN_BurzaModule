using STIN_BurzaModule.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STIN_BurzaModule
{
        public class MoreThanTwoDeclinesFilter : Filter
        {
            public override List<Item> filter(List<Item> items)
            {
                var result = new List<Item>();

                var grouped = items.GroupBy(i => i.getName());

                foreach (var group in grouped)
                {
                    // Vezmi chronologicky (od nejstaršího), jen pracovní dny
                    var ordered = group
                        .OrderBy(i => i.getDate())
                        .Where(i => IsWorkingDay(UnixTimeToDateTime(i.getDate())))
                        .TakeLast(5)
                        .ToList();

                    int declineCount = 0;

                    for (int i = 1; i < ordered.Count; i++)
                    {
                        if (ordered[i].getPrice() < ordered[i - 1].getPrice())
                            declineCount++;
                    }

                    if (declineCount <= 2)
                    {
                        // Firma neměla více než 2 poklesy ⇒ necháme ji ve výsledku
                        result.AddRange(group);
                    }
                    // Jinak firmu vynecháme (více než 2 poklesy)
                }

                return result;
            }

            private DateTime UnixTimeToDateTime(long unixTime) =>
                DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

            private bool IsWorkingDay(DateTime date) =>
                date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
        }
    }
