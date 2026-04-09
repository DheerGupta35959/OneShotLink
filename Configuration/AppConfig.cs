using Microsoft.Extensions.Options;

namespace OneShotLink.Configuration;

public sealed class AppConfig
{
    public string TelegramBotToken { get; set; } = "";
    public string AdminUserIds { get; set; } = "";
    public string UpiId { get; set; } = "";
    public string UpiName { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public int TokenExpiryMinutes { get; set; } = 15;
    public string? DbPath { get; set; }

    public HashSet<long> ParseAdminUserIds()
    {
        var ids = new HashSet<long>();
        foreach (var raw in AdminUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!long.TryParse(raw, out var id) || id <= 0)
            {
                continue;
            }

            ids.Add(id);
        }

        return ids;
    }
}

public sealed class AppConfigValidator : IValidateOptions<AppConfig>
{
    public ValidateOptionsResult Validate(string? name, AppConfig options)
    {
        if (string.IsNullOrWhiteSpace(options.TelegramBotToken))
        {
            return ValidateOptionsResult.Fail("TELEGRAM_BOT_TOKEN is required.");
        }

        if (string.IsNullOrWhiteSpace(options.AdminUserIds))
        {
            return ValidateOptionsResult.Fail("ADMIN_USER_IDS is required.");
        }

        if (options.ParseAdminUserIds().Count == 0)
        {
            return ValidateOptionsResult.Fail("ADMIN_USER_IDS must contain at least one valid Telegram user id.");
        }

        if (string.IsNullOrWhiteSpace(options.UpiId))
        {
            return ValidateOptionsResult.Fail("UPI_ID is required.");
        }

        if (string.IsNullOrWhiteSpace(options.UpiName))
        {
            return ValidateOptionsResult.Fail("UPI_NAME is required.");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail("BASE_URL is required.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl) || (baseUrl.Scheme != "https" && baseUrl.Scheme != "http"))
        {
            return ValidateOptionsResult.Fail("BASE_URL must be a valid absolute URL (http/https).");
        }

        if (options.TokenExpiryMinutes <= 0 || options.TokenExpiryMinutes > 24 * 60)
        {
            return ValidateOptionsResult.Fail("TOKEN_EXPIRY_MINUTES must be between 1 and 1440.");
        }

        return ValidateOptionsResult.Success;
    }
}
