using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using OneShotLink.Handlers;

namespace OneShotLink.Services;

public sealed class BotService(
    ITelegramBotClient botClient,
    UserCommandHandler userCommandHandler,
    AdminCommandHandler adminCommandHandler,
    ILogger<BotService> logger)
{
    public async Task HandleUpdateAsync(Update update, CancellationToken ct)
    {
        try
        {
            if (update.Type != UpdateType.Message)
            {
                return;
            }

            var message = update.Message;
            if (message?.Text is null)
            {
                return;
            }

            var text = message.Text.Trim();
            if (!text.StartsWith('/'))
            {
                return;
            }

            var (command, args) = ParseCommand(text);

            switch (command)
            {
                case "/start":
                    await userCommandHandler.HandleStartAsync(message, ct);
                    return;
                case "/buy":
                    await userCommandHandler.HandleBuyAsync(message, ct);
                    return;
                case "/pending":
                    await adminCommandHandler.HandlePendingAsync(message, ct);
                    return;
                case "/confirm":
                    await adminCommandHandler.HandleConfirmAsync(message, args, ct);
                    return;
                case "/revoke":
                    await adminCommandHandler.HandleRevokeAsync(message, args, ct);
                    return;
                default:
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Unknown command.",
                        cancellationToken: ct);
                    return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle update {UpdateId}", update.Id);
        }
    }

    private static (string Command, string Args) ParseCommand(string text)
    {
        var firstSpace = text.IndexOf(' ');
        var head = firstSpace >= 0 ? text[..firstSpace] : text;
        var args = firstSpace >= 0 ? text[(firstSpace + 1)..].Trim() : "";

        var at = head.IndexOf('@');
        if (at >= 0)
        {
            head = head[..at];
        }

        return (head, args);
    }
}
