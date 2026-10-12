using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockWatcher;

public class StateStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path = Path.GetFullPath("state.json");
    private readonly Dictionary<string, StockStatus> _states;

    public StateStore()
    {
        _states = File.Exists(_path)
            ? JsonSerializer.Deserialize<Dictionary<string, StockStatus>>(File.ReadAllText(_path), Options) ?? new()
            : new();
    }

    public StockStatus? Get(string url) => _states.TryGetValue(url, out var status) ? status : null;

    public void Set(string url, StockStatus status)
    {
        _states[url] = status;
        File.WriteAllText(_path, JsonSerializer.Serialize(_states, Options));
    }
}