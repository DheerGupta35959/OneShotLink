using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneShotLink.Configuration;
using OneShotLink.Data;
using OneShotLink.Endpoints;
using OneShotLink.Handlers;
using OneShotLink.Services;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AppConfig>()
    .Configure<IConfiguration>((cfg, configuration) =>
    {
        cfg.TelegramBotToken = configuration["TELEGRAM_BOT_TOKEN"] ?? "";
        cfg.AdminUserIds = configuration["ADMIN_USER_IDS"] ?? "";
        cfg.UpiId = configuration["UPI_ID"] ?? "";
        cfg.UpiName = configuration["UPI_NAME"] ?? "";
        cfg.BaseUrl = configuration["BASE_URL"] ?? "";

        if (int.TryParse(configuration["TOKEN_EXPIRY_MINUTES"], out var minutes))
        {
            cfg.TokenExpiryMinutes = minutes;
        }

        cfg.DbPath = configuration["DB_PATH"];
    })
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<AppConfig>, AppConfigValidator>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var cfg = sp.GetRequiredService<IOptions<AppConfig>>().Value;
    var dbPath = string.IsNullOrWhiteSpace(cfg.DbPath) ? "oneshot.db" : cfg.DbPath;
    EnsureDbDirectoryExists(dbPath);
    options.UseSqlite($"Data Source={dbPath}");
});

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<AppConfig>>().Value;
    return new TelegramBotClient(cfg.TelegramBotToken);
});

builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserCommandHandler>();
builder.Services.AddScoped<AdminCommandHandler>();
builder.Services.AddScoped<BotService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("OK"));

app.MapPost("/webhook", async (HttpRequest request, BotService botService, CancellationToken ct) =>
{
    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    var update = await request.ReadFromJsonAsync<Update>(jsonOptions, cancellationToken: ct);
    if (update is not null)
    {
        await botService.HandleUpdateAsync(update, ct);
    }

    return Results.Ok();
});

app.MapAccessEndpoint();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var cfg = scope.ServiceProvider.GetRequiredService<IOptions<AppConfig>>().Value;
    var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
    var webhookUrl = $"{cfg.BaseUrl.TrimEnd('/')}/webhook";
    await bot.SetWebhook(
        url: webhookUrl,
        allowedUpdates: [Telegram.Bot.Types.Enums.UpdateType.Message],
        dropPendingUpdates: true);
}

await app.RunAsync();

static void EnsureDbDirectoryExists(string dbPath)
{
    var directory = Path.GetDirectoryName(dbPath);
    if (string.IsNullOrWhiteSpace(directory))
    {
        return;
    }

    Directory.CreateDirectory(directory);
}
