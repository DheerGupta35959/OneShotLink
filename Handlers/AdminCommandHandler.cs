using Microsoft.Extensions.Options;
using OneShotLink.Configuration;
using OneShotLink.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace OneShotLink.Handlers;

public sealed class AdminCommandHandler(
    ITelegramBotClient botClient,
    PaymentService paymentService,
    TokenService tokenService,
    IOptions<AppConfig> config)
{
    public async Task HandlePendingAsync(Message message, CancellationToken ct)
    {
        if (!IsAdmin(message))
        {
            await RejectAsync(message, ct);
            return;
        }

        var pending = await paymentService.GetPendingPaymentsAsync(ct);
        if (pending.Count == 0)
        {
            await botClient.SendMessage(message.Chat.Id, "No pending payments.", cancellationToken: ct);
            return;
        }

        var lines = pending.Select(p =>
        {
            var username = string.IsNullOrWhiteSpace(p.User.Username) ? "(no username)" : $"@{p.User.Username}";
            return $"UserId={p.UserId} Username={username} CreatedAt={p.CreatedAt:O}";
        });

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: string.Join('\n', lines),
            cancellationToken: ct);
    }

    public async Task HandleConfirmAsync(Message message, string args, CancellationToken ct)
    {
        if (!IsAdmin(message))
        {
            await RejectAsync(message, ct);
            return;
        }

        if (!int.TryParse(args, out var userId) || userId <= 0)
        {
            await botClient.SendMessage(message.Chat.Id, "Usage: /confirm <userId>", cancellationToken: ct);
            return;
        }

        var payment = await paymentService.ConfirmPendingPaymentAsync(userId, ct);
        if (payment is null)
        {
            await botClient.SendMessage(message.Chat.Id, "No pending payment found for that userId.", cancellationToken: ct);
            return;
        }

        var token = await tokenService.CreateForUserAsync(payment.UserId, ct);
        var baseUrl = config.Value.BaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/access/{token.Token}";

        await botClient.SendMessage(
            chatId: payment.User.TelegramUserId,
            text: $"Payment confirmed.\n\nYour access link (single-use, expires {token.Expiry:O} UTC):\n{link}",
            cancellationToken: ct);

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: $"Confirmed payment for UserId={payment.UserId}. Token issued and sent to user.",
            cancellationToken: ct);
    }

    public async Task HandleRevokeAsync(Message message, string args, CancellationToken ct)
    {
        if (!IsAdmin(message))
        {
            await RejectAsync(message, ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(args))
        {
            await botClient.SendMessage(message.Chat.Id, "Usage: /revoke <token>", cancellationToken: ct);
            return;
        }

        var result = await tokenService.RevokeAsync(args.Trim(), ct);
        var text = result switch
        {
            RevokeResult.Revoked => "Token revoked (marked used).",
            RevokeResult.NotFound => "Token not found.",
            RevokeResult.AlreadyUsed => "Token already used.",
            RevokeResult.Expired => "Token expired.",
            _ => "Failed to revoke token."
        };

        await botClient.SendMessage(message.Chat.Id, text, cancellationToken: ct);
    }

    private bool IsAdmin(Message message)
    {
        var callerId = message.From?.Id;
        if (callerId is null)
        {
            return false;
        }

        var admins = config.Value.ParseAdminUserIds();
        return admins.Contains(callerId.Value);
    }

    private Task RejectAsync(Message message, CancellationToken ct)
    {
        return botClient.SendMessage(message.Chat.Id, "Unauthorized.", cancellationToken: ct);
    }
}
