namespace OneShotLink.Models;

public sealed class Payment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public User User { get; set; } = null!;
}
