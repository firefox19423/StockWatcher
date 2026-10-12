using System.Net.Http.Json;

namespace StockWatcher;

public class DiscordNotifier(HttpClient http, IConfiguration config, ILogger<DiscordNotifier> logger)
{
    public Task SendRestockAsync(Product product, StockResult result, CancellationToken ct)
    {
        var price = result.Price is decimal p ? $"{p:0.00} $" : "price unknown";

        var payload = new
        {
            content = $"{Mention()}**{product.Name}** is in stock!",
            allowed_mentions = new { users = Users() },
            embeds = new[]
            {
                new { title = result.Title, url = product.Url, description = $"Price: {price}", color = 0x2ECC71 }
            }
        };
        return PostAsync(payload, ct);
    }

    public Task SendWarningAsync(string message, CancellationToken ct)
    {
        var payload = new
        {
            content = $"{Mention()}{message}",
            allowed_mentions = new { users = Users() }
        };
        return PostAsync(payload, ct);
    }

    private string Mention() =>
        string.IsNullOrWhiteSpace(config["Discord:UserId"]) ? "" : $"<@{config["Discord:UserId"]}> ";

    private string[] Users() =>
        string.IsNullOrWhiteSpace(config["Discord:UserId"]) ? [] : [config["Discord:UserId"]!];

    private async Task PostAsync(object payload, CancellationToken ct)
    {
        var webhookUrl = config["Discord:WebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogError("Discord:WebhookUrl is not set. Check Manage User Secrets.");
            return;
        }

        try
        {
            using var response = await http.PostAsJsonAsync(webhookUrl, payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Discord returned {Status}: {Body}", (int)response.StatusCode, body);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not reach Discord");
        }
    }
}