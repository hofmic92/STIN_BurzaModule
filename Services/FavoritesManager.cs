using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace STIN_BurzaModule
{
    public class FavoritesManager
    {
        private readonly string _filePath;
        private readonly IConfiguration _config;

        public FavoritesManager(IConfiguration config)
        {
            _config = config;
            _filePath = "favorites.json";
            // Initialize with default favorites from appsettings if file doesn't exist
            if (!File.Exists(_filePath))
            {
                var defaultFavorites = _config.GetSection("Favorites").Get<List<string>>();
                File.WriteAllText(_filePath, JsonSerializer.Serialize(defaultFavorites));
            }
        }

        public List<string> GetFavorites()
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        public void SaveFavorites(List<string> favorites)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(favorites));
        }

        public void ClearStorage()
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(new List<string>()));
        }
    }
}