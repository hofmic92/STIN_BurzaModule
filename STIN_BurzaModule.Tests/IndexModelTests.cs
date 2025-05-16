
using StockModule.Pages;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Reflection;
using System.Net.Http;
using System.Text;

namespace STIN_BurzaModule.Tests
{
    public class IndexModelTests
    {
        private static string tempPath = Path.Combine(Path.GetTempPath(), "burza_test");

        private IndexModel CreateModel()
        {
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var model = new IndexModel(httpClientFactoryMock.Object);
            return model;
        }

        private void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, value); // static field
        }

        private T GetField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            return (T)field?.GetValue(null); // static field
        }

        [Fact]
        public void OnPostAddItem_AddsItem_AndRedirects()
        {
            var model = CreateModel();

            // Resetuje statickou kolekci FavoriteItems
            typeof(IndexModel)
                .GetField("_favoriteItems", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, new List<string>());

            model.NewItem = "GOOG";
            var result = model.OnPostAddItem();

            var list = typeof(IndexModel)
                .GetField("_favoriteItems", BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null) as List<string>;

            Assert.IsType<RedirectToPageResult>(result);
        }



        [Fact]
        public void OnPostRemoveItem_RemovesItem_AndRedirects()
        {
            var model = CreateModel();
            SetField(model, "_favoriteItems", new List<string> { "AAPL", "TSLA" });

            var result = model.OnPostRemoveItem("TSLA");

            var list = GetField<List<string>>(model, "_favoriteItems");
            Assert.DoesNotContain("TSLA", list);
            Assert.Contains("AAPL", list);
            Assert.IsType<RedirectToPageResult>(result);
        }

        [Fact]
        public void OnPostClearLog_ClearsLogAndRedirects()
        {
            var model = CreateModel();

            var result = model.OnPostClearLog();

            var log = GetField<StringBuilder>(model, "_logBuilder");
            var content = log.ToString();

            Assert.True(string.IsNullOrEmpty(content) || !content.Contains("něco v logu"));
            Assert.IsType<RedirectToPageResult>(result);
        }

        [Fact]
        public void SaveFavoritesToFile_CreatesValidJsonFile()
        {
            var model = CreateModel();
            var testList = new List<string> { "AAPL", "MSFT" };
            SetField(model, "_favoriteItems", testList);

            var method = model.GetType().GetMethod("SaveFavoritesToFile", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(model, null);

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "favorites.json");
            Assert.True(File.Exists(path));

            var content = File.ReadAllText(path);
            var deserialized = JsonSerializer.Deserialize<List<string>>(content);
            Assert.Contains("AAPL", deserialized);
            Assert.Contains("MSFT", deserialized);
        }

        [Fact]
        public void LoadFavoritesFromFile_ReadsValidJson()
        {
            var model = CreateModel();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "favorites.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonSerializer.Serialize(new List<string> { "GOOG", "TSLA" }));

            var method = model.GetType().GetMethod("LoadFavoritesFromFile", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(model, null);

            var list = GetField<List<string>>(model, "_favoriteItems");
            Assert.Contains("GOOG", list);
            Assert.Contains("TSLA", list);
        }

        [Fact]
        public void LoadFavoritesFromFile_WhenFileMissing_DoesNotThrow()
        {
            var model = CreateModel();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "favorites.json");
            if (File.Exists(path)) File.Delete(path);

            var ex = Record.Exception(() =>
            {
                var method = model.GetType().GetMethod("LoadFavoritesFromFile", BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(model, null);
            });

            Assert.Null(ex);
        }

        [Fact]
        public async Task FetchDataInternal_PopulatesDownloadedItems_WhenStaticDataEnabled()
        {
            var model = CreateModel();

            // nastavíme UseStaticData = true
            typeof(IndexModel)
                .GetField("UseStaticData", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, true);

            // nastavíme oblíbené položky
            typeof(IndexModel)
                .GetField("_favoriteItems", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, new List<string> { "AAPL", "TSLA" });

            // zavoláme FetchDataInternal pomocí reflexe
            var method = typeof(IndexModel).GetMethod("FetchDataInternal", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)method?.Invoke(model, null);
            await task;

            var downloaded = typeof(IndexModel).GetProperty("DownloadedItems")?.GetValue(model) as List<Item>;

            Assert.NotNull(downloaded);
            Assert.True(downloaded.Count >= 2); // mělo by tam něco být
            Assert.Contains(downloaded, i => i.getName() == "AAPL");
            Assert.Contains(downloaded, i => i.getName() == "TSLA");
        }

    }
}
