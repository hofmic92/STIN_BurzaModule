using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Collections.Generic;

namespace StockModule.Pages
{
    public class IndexModel : PageModel
    {
        private static List<string> _favoriteItems = new();
        private static StringBuilder _logBuilder = new();

        [BindProperty]
        public string? NewItem { get; set; }

        public List<string> FavoriteItems => _favoriteItems;
        public string LogOutput => _logBuilder.ToString();

        public void OnGet()
        {
            Log("Na�ten� str�nky Index");
        }

        public IActionResult OnPostAddItem()
        {
            if (!string.IsNullOrWhiteSpace(NewItem))
            {
                var itemToAdd = NewItem.Trim().ToUpper();
                if (!_favoriteItems.Contains(itemToAdd))
                {
                    _favoriteItems.Add(itemToAdd);
                    Log($"Polo�ka p�id�na: {itemToAdd}");
                }
            }
            return RedirectToPage();
        }

        public IActionResult OnPostRemoveItem(string item)
        {
            if (!string.IsNullOrEmpty(item) && _favoriteItems.Contains(item))
            {
                _favoriteItems.Remove(item);
                Log($"Polo�ka odebr�na: {item}");
            }
            return RedirectToPage();
        }

        public IActionResult OnPostClearLog()
        {
            _logBuilder.Clear();
            Log("Log byl vymaz�n");
            return RedirectToPage();
        }

        private void Log(string message)
        {
            var logEntry = $"{DateTime.Now:HH:mm:ss} | {message}";
            _logBuilder.AppendLine(logEntry);
        }
    }
}