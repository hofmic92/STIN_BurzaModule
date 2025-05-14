using STIN_BurzaModule.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STIN_BurzaModule
{
    public class DeclineThreeDaysFilter : Filter
    {
        public override List<Item> filter(List<Item> items)
        {
            var result = new List<Item>();

            // Skupiny podle jména firmy
            var grouped = items.GroupBy(i => i.getName());

            foreach (var group in grouped)
            {
                var ordered = group
                    .OrderByDescending(i => i.getDate())
                    .Where(i => IsWorkingDay(UnixTimeToDateTime(i.getDate())))
                    .Take(3)
                    .ToList();

                if (ordered.Count < 3)
                {
                    result.AddRange(group);
                    continue;
                }

                bool allDeclined = true;
                for (int i = 1; i < ordered.Count; i++)
                {
                    if (ordered[i].getPrice() >= ordered[i - 1].getPrice())
                    {
                        allDeclined = false;
                        break;
                    }
                }

                if (!allDeclined)
                    result.AddRange(group); // firma zůstává
            }

            return result;
        }

        private DateTime UnixTimeToDateTime(long unixTime) =>
            DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

        private bool IsWorkingDay(DateTime date) =>
            date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }
}
