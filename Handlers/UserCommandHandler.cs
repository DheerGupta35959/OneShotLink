using Microsoft.Extensions.Options;
using OneShotLink.Configuration;
using OneShotLink.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace OneShotLink.Handlers;

public sealed class UserCommandHandler(
    ITelegramBotClient botClient,
    PaymentService paymentService,
    IOptions<AppConfig> config)
{
    public Task HandleStartAsync(Message message, CancellationToken ct)
    {
        var text =
            "Welcome.\n\n" +
            "Flow:\n" +
            "1) Send /buy to get UPI payment details.\n" +
            "2) Pay manually via UPI.\n" +
            "3) After admin confirmation, you will receive a single-use access link.\n\n" +
            "The access link expires automatically and can be opened only once.";

        return botClient.SendMessage(
            chatId: message.Chat.Id,
            text: text,
            cancellationToken: ct);
    }

    public async Task HandleBuyAsync(Message message, CancellationToken ct)
    {
        var from = message.From;
        if (from is null)
        {
            return;
        }

        var user = await paymentService.UpsertUserAsync(from.Id, from.Username, ct);
        var payment = await paymentService.CreatePendingIfNoneAsync(user.Id, ct);

        if (payment is null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "You already have a pending payment. Please complete the UPI payment and wait for confirmation.",
                cancellationToken: ct);
            return;
        }

        var cfg = config.Value;
        var paymentText =
            "Send payment via UPI using the details below:\n\n" +
            $"UPI ID: {cfg.UpiId}\n" +
            $"Name: {cfg.UpiName}\n\n" +
            "After you pay, wait for an admin to confirm. You will receive a single-use, expiring access link in this chat.";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: paymentText,
            cancellationToken: ct);
    }
}
