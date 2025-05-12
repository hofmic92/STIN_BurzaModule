using STIN_BurzaModule;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapGet("/new/rate", () =>
{
    List<Item> items = new List<Item>
{
    new Item { Name = "Microsoft", Date = 1713960000, Rating = null, Sell = null },
    new Item { Name = "Google", Date = 1713960001, Rating = null, Sell = null },
    new Item { Name = "Amazon", Date = 1713960002, Rating = null, Sell = null },
    new Item { Name = "Tesla", Date = 1713960003, Rating = null, Sell = null },
    new Item { Name = "Apple", Date = 1713960004, Rating = null, Sell = null },
    new Item { Name = "Meta", Date = 1713960005, Rating = null, Sell = null },
    new Item { Name = "NVIDIA", Date = 1713960006, Rating = null, Sell = null },
    new Item { Name = "Intel", Date = 1713960007, Rating = null, Sell = null },
    new Item { Name = "Netflix", Date = 1713960008, Rating = null, Sell = null },
    new Item { Name = "Adobe", Date = 1713960009, Rating = null, Sell = null },
    new Item { Name = "IBM", Date = 1713960010, Rating = null, Sell = null },
    new Item { Name = "AMD", Date = 1713960011, Rating = null, Sell = null },
    new Item { Name = "Oracle", Date = 1713960012, Rating = null, Sell = null },
    new Item { Name = "Salesforce", Date = 1713960013, Rating = null, Sell = null },
    new Item { Name = "Spotify", Date = 1713960014, Rating = null, Sell = null },
    new Item { Name = "Twitter", Date = 1713960015, Rating = null, Sell = null },
    new Item { Name = "Uber", Date = 1713960016, Rating = null, Sell = null },
    new Item { Name = "Airbnb", Date = 1713960017, Rating = null, Sell = null },
    new Item { Name = "Zoom", Date = 1713960018, Rating = null, Sell = null },
    new Item { Name = "OpenAI", Date = 1713960019, Rating = null, Sell = null }
};
    //testovací, nahradí se reálným listem itemù který se nahraje a to co neznáme bude null

    return items; // tohle zajistí JSON serializaci
})
.WithName("GetItems")
.WithOpenApi();

app.MapRazorPages();



app.Run();
