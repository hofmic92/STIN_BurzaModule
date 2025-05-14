namespace STIN_BurzaModule
{
    using Microsoft.Extensions.Hosting;
    using System.Threading;
    using System.Threading.Tasks;
    using System;
    using StockModule.Pages;

    public class StockDataBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<StockDataBackgroundService> _logger;

        public StockDataBackgroundService(IServiceProvider services, ILogger<StockDataBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background služba spuštěna");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                _logger.LogInformation($"Další spuštění v {nextRun:HH:mm:ss} (za {delay.TotalMinutes:N0} minut)");

                await Task.Delay(delay, stoppingToken);

                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var model = scope.ServiceProvider.GetRequiredService<IndexModel>();
                        await model.FetchDataInternal();
                        _logger.LogInformation("Automatické stahování dokončeno");

                        // Send data after fetch
                        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                        var client = httpClientFactory.CreateClient();
                        var items = model.GetStockItems();

                        try
                        {
                            var response = await client.PostAsJsonAsync("https://stinnews-cpaeakbfgkdpe0ae.westeurope-01.azurewebsites.net/UserDashboard", items);
                            response.EnsureSuccessStatusCode();
                            _logger.LogInformation("Data úspěšně odeslána");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Chyba při odesílání dat");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Chyba při automatickém stahování");
                }
            }
        }

        private DateTime GetNextRunTime(DateTime now)
        {
            var scheduledTimes = new[]
            {
        new TimeSpan(0, 0, 30),   // 00:00
        new TimeSpan(0, 1, 0)    // 06:00
        //new TimeSpan(12, 0, 0),   // 12:00
        //new TimeSpan(18, 0, 0)    // 18:00
    };

            foreach (var time in scheduledTimes)
            {
                var nextRun = now.Date.Add(time);
                if (now < nextRun)
                {
                    return nextRun;
                }
            }

            return now.Date.AddDays(1).Add(scheduledTimes[0]);
        }
    }
}