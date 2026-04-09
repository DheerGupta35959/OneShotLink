namespace OneShotLink.Models;

public sealed class AccessToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public int UserId { get; set; }
    public bool IsUsed { get; set; }
    public DateTime Expiry { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
