using Microsoft.AspNetCore.Mvc;
using STIN_BurzaModule;
using STIN_BurzaModule.DataModel;
using StockModule.Pages;
using System.Text;
using System.Text.Json;
using STIN_BurzaModule.DataModel;
using STIN_BurzaModule.Pages;
using STIN_BurzaModule.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using STIN_BurzaModule.Filters;
using STIN_BurzaModule.ConfigClasses;
using Microsoft.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Registrace služeb
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<StockDataBackgroundService>();
builder.Services.AddTransient<IndexModel>();
builder.Services.AddScoped<DeclineThreeDaysFilter>();
builder.Services.AddScoped<MoreThanTwoDeclinesFilter>();
builder.Services.AddScoped<FinalFilter>();
builder.Services.Configure<UserFilterSettings>(builder.Configuration.GetSection("UserFilters"));
builder.Services.Configure<UrlSetting>(builder.Configuration.GetSection("Url"));
builder.Services.Configure<SellValueSetting>(builder.Configuration.GetSection("SellValueSetting"));

// Build aplikace
var app = builder.Build();

// Konfigurace middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

var configuration = builder.Configuration;

var urlConfig = configuration.GetSection("Url").Get<UrlSetting>();
var url = urlConfig.EnableUrl;

// Získání hodnoty z konfigurace
//string url = builder.Configuration["RatingService:Url"];

app.MapPost("/liststock", async (IServiceProvider services) =>
{
    using var scope = services.CreateScope();
    var model = scope.ServiceProvider.GetRequiredService<IndexModel>();
    await model.FetchDataInternal();

    List<DataModel> dataModels = new List<DataModel>();
    List<Item> items = model.DownloadedItems;
    if (items == null)
        {
        }
        else
        {
            
            var filterSettings = configuration.GetSection("UserFilters").Get<UserFilterSettings>();

            var filters = new List<Filter>();

            if (filterSettings.EnableDeclineThreeDays)
                filters.Add(new DeclineThreeDaysFilter());

            if (filterSettings.EnableMoreThanTwoDeclines)
                filters.Add(new MoreThanTwoDeclinesFilter());
            
            filters.Add(new FinalFilter()); // vždy nech nejnovější záznam
            foreach (Filter filter in filters)
            {
            items = filter.filter(items);
            }
        }
    //foreach (var item in model.FilteredItems)
    //{
    //    Console.WriteLine($"DEBUG: Name={item.getName()}, Date={item.getDate()}, Price={item.getPrice()}, Sell={item.getSell()}, Rating={item.getRating()}");
    //}
    foreach (Item item in items)
    {
        DataModel datamodel = new DataModel();
        datamodel.Sell = item.getSell();
        datamodel.Name = item.getName();
        datamodel.Date = item.getDate();
        datamodel.Rating = item.getRating();
        dataModels.Add(datamodel);
        Console.WriteLine($"Processing: {datamodel.Name}, Date: {datamodel.Date}, Rating: {datamodel.Rating}, Sell: {datamodel.Sell}");
    }
    
    Console.WriteLine(dataModels);
    Console.WriteLine("????????????????");

    return Results.Ok(dataModels);
})
.WithName("PostListStockStatic")
.WithOpenApi();



/*app.MapPost("/liststock-static", async () =>
{

    //var items = GetStockItems();


    string jsonString = @"[
    {
        ""name"": ""JoeMama"", 
        ""date"": 12345678, 
        ""rating"": -10, 
        ""sell"": 1
    },
    {
        ""name"": ""Google"", 
        ""date"": 12345678, 
        ""rating"": 10, 
        ""sell"": 0
    },
    {

        ""name"": ""OpenAI"", 
        ""date"": 12345678, 
        ""rating"": 2, 
        ""sell"": 0

    }
]";


    List<DataModel> data = JsonSerializer.Deserialize<List<DataModel>>(jsonString);
    return Results.Ok(data);
});*/




app.MapPost("/salestock", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    // Deserializace do vlastní C# třídy (ne JsonElement!)
    var data = JsonSerializer.Deserialize<List<DataModel>>(body, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    if (data == null)
    {
        return Results.BadRequest("Špatná data");
    }

    var sellValueConfig = configuration.GetSection("SellValueSetting").Get<SellValueSetting>();
    int sellValue = sellValueConfig.SellOrNo;

    var items = new List<Item>();

    foreach (var element in data)
    {
        var name = element.Name ?? "";
        var date = element.Date;
        var rating = element.Rating ;
        Console.WriteLine("Sell value is " + sellValue);
        var item = new Item(name, date, rating);
        item.setSellValue(sellValue);
        item.setSell();
        items.Add(item);
    }
    data.Clear();
    foreach (Item item in items)
    {
        DataModel datamodel = new DataModel();
        datamodel.Sell = item.getSell();
        datamodel.Name = item.getName();
        datamodel.Date = item.getDate();
        datamodel.Rating = item.getRating();
        data.Add(datamodel);
        Console.WriteLine($"Processing: {datamodel.Name}, Date: {datamodel.Date}, Rating: {datamodel.Rating}, Sell: {datamodel.Sell}");
    }

    return Results.Ok(data); // Můžeš vracet i původní `data`, ale zde vracíme už upravené Itemy
});

/*app.MapPost("/salestock-static", async () =>
{


    //var items = GetStockItems();

    string jsonString = @"[
    {
        ""name"": ""JoeMama"", 
        ""date"": 12345678, 
        ""rating"": -10, 
        ""sell"": 1
    },
    {
        ""name"": ""Google"", 
        ""date"": 12345678, 
        ""rating"": 10, 
        ""sell"": 0
    },
    {
        ""name"": ""OpenAI"", 
        ""date"": 12345678, 
        ""rating"": 2, 
        ""sell"": 0
    }
]";

    List<DataModel> data = JsonSerializer.Deserialize<List<DataModel>>(jsonString);


    return Results.Ok(data);


});*/







// Fetch latest data
//await model.FetchDataInternal();

// Get data in JSON format
//var items = model.GetStockItems();
//Console.WriteLine(items);
//onsole.WriteLine("kkkkkkkkkkkkkkkk");





app.MapRazorPages();

app.Run(); // Spuštění aplikace

// Supporting classes





