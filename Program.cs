using Microsoft.AspNetCore.Mvc;
using STIN_BurzaModule;
using STIN_BurzaModule.DataModel;
using StockModule.Pages;
using System.Text;
using System.Text.Json;
using STIN_BurzaModule.DataModel;
using STIN_BurzaModule.Pages;

var builder = WebApplication.CreateBuilder(args);

// Registrace služeb
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<StockDataBackgroundService>();
builder.Services.AddTransient<IndexModel>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<CommunicationManager>();
builder.Services.AddScoped<DeclineThreeDaysFilter>();
builder.Services.AddScoped<MoreThanTwoDeclinesFilter>();
builder.Services.AddScoped<FinalFilter>();
builder.Services.Configure<UserFilterSettings>(builder.Configuration.GetSection("UserFilters"));

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

// Endpointy
app.MapGet("/neco", () =>
{
    List<DataModel> items = new List<DataModel>
    {
        new Item("Microsoft", 1713960000, 0) { Rating = null, Sell = null }
    };
    return items;
})
.WithName("GetItems")
.WithOpenApi();


var url = "https://stinnews-cpaeakbfgkdpe0ae.westeurope-01.azurewebsites.net/rating";

app.MapPost("/liststock-static", async () =>
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
});



app.MapPost("/salestock-static", async () =>
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


});





// Fetch latest data
//await model.FetchDataInternal();

// Get data in JSON format
//var items = model.GetStockItems();
//Console.WriteLine(items);
//onsole.WriteLine("kkkkkkkkkkkkkkkk");





app.MapRazorPages();

app.Run(); // Spuštění aplikace

// Supporting classes



