using Microsoft.EntityFrameworkCore;
using OneShotLink.Data;
using OneShotLink.Models;

namespace OneShotLink.Services;

public sealed class PaymentService(AppDbContext db)
{
    public async Task<User> UpsertUserAsync(long telegramUserId, string? username, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);
        if (user is null)
        {
            user = new User
            {
                TelegramUserId = telegramUserId,
                Username = string.IsNullOrWhiteSpace(username) ? null : username,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            return user;
        }

        var normalized = string.IsNullOrWhiteSpace(username) ? null : username;
        if (user.Username != normalized)
        {
            user.Username = normalized;
            await db.SaveChangesAsync(ct);
        }

        return user;
    }

    public async Task<Payment?> CreatePendingIfNoneAsync(int userId, CancellationToken ct)
    {
        var hasPending = await db.Payments.AnyAsync(p => p.UserId == userId && p.Status == PaymentStatus.Pending, ct);
        if (hasPending)
        {
            return null;
        }

        var payment = new Payment
        {
            UserId = userId,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ConfirmedAt = null
        };

        db.Payments.Add(payment);
        try
        {
            await db.SaveChangesAsync(ct);
            return payment;
        }
        catch (DbUpdateException)
        {
            return null;
        }
    }

    public Task<List<Payment>> GetPendingPaymentsAsync(CancellationToken ct)
    {
        return db.Payments
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.Status == PaymentStatus.Pending)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Payment?> ConfirmPendingPaymentAsync(int userId, CancellationToken ct)
    {
        var payment = await db.Payments
            .Include(p => p.User)
            .Where(p => p.UserId == userId && p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (payment is null)
        {
            return null;
        }

        payment.Status = PaymentStatus.Confirmed;
        payment.ConfirmedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return payment;
    }
}
