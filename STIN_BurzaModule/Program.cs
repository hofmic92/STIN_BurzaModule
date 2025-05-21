using Microsoft.AspNetCore.Mvc;
using STIN_BurzaModule;
using STIN_BurzaModule.DataModel;
using StockModule.Pages;
using System.Text;
using System.Text.Json;
using STIN_BurzaModule.Services;
using STIN_BurzaModule.Filters;
using STIN_BurzaModule.ConfigClasses;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
app.UseAuthorization();

var configuration = builder.Configuration;

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
    foreach (Item item in items)
    {
        DataModel datamodel = new DataModel();
        datamodel.Sell = item.getSell();
        datamodel.Name = item.getName();
        datamodel.Date = item.getDate();
        datamodel.Rating = item.getRating();
        dataModels.Add(datamodel);
    }
    return Results.Ok(dataModels);
})
.WithName("PostListStockStatic")
.WithOpenApi();
app.MapPost("/salestock", async (HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var inputItems = JsonSerializer.Deserialize<List<DataModel>>(body, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });
    if (inputItems == null)
    {
        return Results.BadRequest("Neplatný vstupní formát dat");
    }
    var sellValueConfig = configuration.GetSection("SellValueSetting").Get<SellValueSetting>();
    int sellValue = sellValueConfig.SellOrNo;
    var ratingServiceUrl = configuration["Url:RatingService"];
    var ratingRequestData = inputItems.Select(i => new {
        name = i.Name,
        date = i.Date
    }).ToList();
    var httpClient = httpClientFactory.CreateClient();
    List<DataModel> ratedItems = new();
    try
    {
        var ratingResponse = await httpClient.PostAsJsonAsync(ratingServiceUrl, ratingRequestData);

        if (!ratingResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Chyba při získávání ratingů: {ratingResponse.StatusCode}");
        }
        ratedItems = await ratingResponse.Content.ReadFromJsonAsync<List<DataModel>>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Chyba při komunikaci s rating API: {ex.Message}");
        return Results.Problem("Nelze získat ratingy z externí služby");
    }
    var resultItems = new List<DataModel>();
    foreach (var ratedItem in ratedItems)
    {
        var item = new Item(ratedItem.Name, ratedItem.Date);
        item.setRating(ratedItem.Rating);
        item.setSellValue(sellValue);
        item.setSell();
        resultItems.Add(new DataModel
        {
            Name = item.getName(),
            Date = item.getDate(),
            Rating = item.getRating(),
            Sell = item.getSell()
        });
    }
    return Results.Ok(resultItems);
});
app.MapPost("/rating", async (HttpClient httpClient, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<List<DataModel>>(body, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    if (data == null)
        return Results.BadRequest("Špatná data");
    var urlConfig = configuration.GetSection("Url").Get<UrlSetting>();
    var ratingServiceUrl = urlConfig.EnableUrl;
    var response = await httpClient.PostAsync(
        ratingServiceUrl,
        new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"));

    if (!response.IsSuccessStatusCode)
        return Results.Problem("Chyba při volání rating služby");

    var responseData = await response.Content.ReadAsStringAsync();
    var updatedData = JsonSerializer.Deserialize<List<DataModel>>(responseData);

    return Results.Ok(updatedData);
});
app.MapRazorPages();
app.Run(); // Spuštění aplikace
