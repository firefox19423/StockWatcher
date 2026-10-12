namespace StockWatcher;

public class Worker(
    ILogger<Worker> logger,
    IConfiguration config,
    ShopifyChecker checker,
    DiscordNotifier discord,
    StateStore state) : BackgroundService
{
    private readonly Dictionary<string, int> _failures = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var products = config.GetSection("Products").Get<List<Product>>() ?? [];
        var interval = TimeSpan.FromMinutes(config.GetValue("CheckIntervalMinutes", 3));

        logger.LogInformation("Watching {Count} product(s) every {Minutes} min",
            products.Count, interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var product in products)
            {
                try
                {
                    await CheckProductAsync(product, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Unexpected error for {Name}", product.Name);
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); // be gentle with the store
            }

            // A few random seconds so it doesn't run like a perfect timer
            var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 30));
            await Task.Delay(interval + jitter, stoppingToken);
        }
    }

    private async Task CheckProductAsync(Product product, CancellationToken ct)
    {
        var result = await checker.CheckAsync(product.Url, ct);

        if (result.Status == StockStatus.Unknown)
        {
            var count = _failures.GetValueOrDefault(product.Url) + 1;
            _failures[product.Url] = count;
            logger.LogWarning("{Name}: UNKNOWN ({Reason}), failure #{Count}", product.Name, result.Reason, count);

            if (count == 10)
                await discord.SendWarningAsync(
                    $"⚠️ I can't check **{product.Name}** anymore ({result.Reason}). Is the link still valid?", ct);
            return;
        }

        _failures[product.Url] = 0;
        var previous = state.Get(product.Url);

        logger.LogInformation("{Name}: {Status}, {Price}$", product.Name, result.Status, result.Price);

        if (result.Status == StockStatus.InStock && previous != StockStatus.InStock)
        {
            logger.LogInformation("{Name} is in stock: sending Discord ping", product.Name);
            await discord.SendRestockAsync(product, result, ct);
        }

        state.Set(product.Url, result.Status);
    }
}