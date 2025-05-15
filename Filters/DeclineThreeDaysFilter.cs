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

            var grouped = items.GroupBy(i => i.getName());

            foreach (var group in grouped)
            {
                // Vezmi posledních 3 pracovních dnů, seřazené chronologicky
                var ordered = group
                    .OrderBy(i => i.getDate())
                    .Where(i => IsWorkingDay(UnixTimeToDateTime(i.getDate())))
                    .TakeLast(3)
                    .ToList();

                if (ordered.Count < 3)
                {
                    // Pokud nemáme dost dat, firmu nefiltrujeme
                    result.AddRange(group);
                    continue;
                }

                bool declined = true;
                for (int i = 1; i < ordered.Count; i++)
                {
                    if (ordered[i].getPrice() >= ordered[i - 1].getPrice())
                    {
                        declined = false;
                        break;
                    }
                }

                if (!declined)
                {
                    // Firma neklesala 3 dny po sobě ⇒ necháme všechny její záznamy
                    result.AddRange(group);
                }
                // Jinak firmu vynecháme
            }

            return result;
        }

        private DateTime UnixTimeToDateTime(long unixTime) =>
            DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

        private bool IsWorkingDay(DateTime date) =>
            date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }
}
