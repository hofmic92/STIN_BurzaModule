using STIN_BurzaModule;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


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
    //testovac�, nahrad� se re�ln�m listem item� kter� se nahraje a to co nezn�me bude null

    return items; // tohle zajist� JSON serializaci
})
.WithName("GetItems")
.WithOpenApi();


app.Run();


