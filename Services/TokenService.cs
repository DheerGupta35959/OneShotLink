using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneShotLink.Configuration;
using OneShotLink.Data;
using OneShotLink.Models;

namespace OneShotLink.Services;

public sealed class TokenService(AppDbContext db, IOptions<AppConfig> config)
{
    public async Task<AccessToken> CreateForUserAsync(int userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(config.Value.TokenExpiryMinutes);

        while (true)
        {
            var tokenValue = GenerateTokenValue();

            var exists = await db.AccessTokens.AnyAsync(t => t.Token == tokenValue, ct);
            if (exists)
            {
                continue;
            }

            var token = new AccessToken
            {
                Token = tokenValue,
                UserId = userId,
                IsUsed = false,
                Expiry = expiry,
                CreatedAt = now
            };

            db.AccessTokens.Add(token);
            await db.SaveChangesAsync(ct);
            return token;
        }
    }

    public async Task<RevokeResult> RevokeAsync(string tokenValue, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var token = await db.AccessTokens.FirstOrDefaultAsync(t => t.Token == tokenValue, ct);
        if (token is null)
        {
            return RevokeResult.NotFound;
        }

        if (token.IsUsed)
        {
            return RevokeResult.AlreadyUsed;
        }

        if (token.Expiry <= now)
        {
            return RevokeResult.Expired;
        }

        token.IsUsed = true;
        await db.SaveChangesAsync(ct);
        return RevokeResult.Revoked;
    }

    private static string GenerateTokenValue()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}

public enum RevokeResult
{
    Revoked = 0,
    NotFound = 1,
    AlreadyUsed = 2,
    Expired = 3
}
