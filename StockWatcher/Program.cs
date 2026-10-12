using StockWatcher;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("StockWatcher/1.0 (personal restock checker)");

builder.Services.AddSingleton(http);
builder.Services.AddSingleton<ShopifyChecker>();
builder.Services.AddSingleton<DiscordNotifier>();
builder.Services.AddSingleton<StateStore>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();