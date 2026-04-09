using System.Data;
using Microsoft.EntityFrameworkCore;
using OneShotLink.Data;

namespace OneShotLink.Endpoints;

public static class AccessEndpoint
{
    public static IEndpointRouteBuilder MapAccessEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/access/{token}", HandleAsync);
        return app;
    }

    private static async Task<IResult> HandleAsync(string token, AppDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var tokenInfo = await db.AccessTokens
            .AsNoTracking()
            .Where(t => t.Token == token)
            .Select(t => new { t.IsUsed, t.Expiry })
            .FirstOrDefaultAsync(ct);

        if (tokenInfo is null)
        {
            return Results.NotFound();
        }

        if (tokenInfo.IsUsed)
        {
            return Results.StatusCode(StatusCodes.Status410Gone);
        }

        if (tokenInfo.Expiry <= now)
        {
            return Results.StatusCode(StatusCodes.Status410Gone);
        }

        var affected = await db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE AccessTokens SET IsUsed = 1 WHERE Token = {token} AND IsUsed = 0 AND Expiry > {now};",
            ct);

        if (affected != 1)
        {
            return Results.StatusCode(StatusCodes.Status410Gone);
        }

        await tx.CommitAsync(ct);

        return Results.Ok("Access granted.");
    }
}
