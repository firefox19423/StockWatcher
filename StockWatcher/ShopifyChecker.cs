using System.Text.Json;
using System.Web;

namespace StockWatcher;

public enum StockStatus { InStock, OutOfStock, Unknown }

public record StockResult(StockStatus Status, string Title, decimal? Price, string Reason);

public class ShopifyChecker(HttpClient http)
{
    public async Task<StockResult> CheckAsync(string productUrl, CancellationToken ct)
    {
        try
        {
            var uri = new Uri(productUrl);

            // https://store.com/products/abc?variant=123  ->  https://store.com/products/abc.js
            var path = uri.AbsolutePath.TrimEnd('/');
            if (!path.EndsWith(".js")) path += ".js";
            var jsUrl = $"{uri.Scheme}://{uri.Host}{path}";

            // If the link points to a specific variant (e.g. a card condition), check that one
            long? variantId = long.TryParse(HttpUtility.ParseQueryString(uri.Query)["variant"], out var id) ? id : null;

            using var response = await http.GetAsync(jsUrl, ct);
            if (!response.IsSuccessStatusCode)
                return new(StockStatus.Unknown, "", null, $"HTTP {(int)response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync(ct);
            return Parse(json, variantId);
        }
        catch (HttpRequestException ex)
        {
            return new(StockStatus.Unknown, "", null, $"Network error: {ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new(StockStatus.Unknown, "", null, "Timed out");
        }
        catch (UriFormatException)
        {
            return new(StockStatus.Unknown, "", null, "Invalid URL in appsettings.json");
        }
    }

    public static StockResult Parse(string json, long? variantId = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var target = root;

            if (variantId is not null)
            {
                var found = false;
                if (root.TryGetProperty("variants", out var variants))
                {
                    foreach (var v in variants.EnumerateArray())
                    {
                        if (v.TryGetProperty("id", out var vid) && vid.GetInt64() == variantId)
                        {
                            target = v;
                            found = true;
                            if (v.TryGetProperty("title", out var vt)) title += $" ({vt.GetString()})";
                            break;
                        }
                    }
                }
                if (!found)
                    return new(StockStatus.Unknown, title, null, $"Variant {variantId} not found");
            }

            if (!target.TryGetProperty("available", out var available)
                || available.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return new(StockStatus.Unknown, title, null, "No 'available' field");

            // Shopify gives prices in cents
            decimal? price = target.TryGetProperty("price", out var p) && p.ValueKind == JsonValueKind.Number
                ? p.GetDecimal() / 100m
                : null;

            var status = available.GetBoolean() ? StockStatus.InStock : StockStatus.OutOfStock;
            return new(status, title, price, "OK");
        }
        catch (JsonException)
        {
            return new(StockStatus.Unknown, "", null, "Response was not JSON (blocked or not a Shopify store?)");
        }
    }
}