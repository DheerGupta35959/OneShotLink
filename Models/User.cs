namespace OneShotLink.Models;

public sealed class User
{
    public int Id { get; set; }
    public long TelegramUserId { get; set; }
    public string? Username { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Payment> Payments { get; set; } = [];
    public List<AccessToken> AccessTokens { get; set; } = [];
}
