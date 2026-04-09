using Microsoft.EntityFrameworkCore;
using OneShotLink.Models;

namespace OneShotLink.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AccessToken> AccessTokens => Set<AccessToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.TelegramUserId).IsUnique();
            b.Property(x => x.Username).HasMaxLength(64);
            b.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId).IsUnique().HasFilter("Status = 0");
            b.HasIndex(x => new { x.UserId, x.Status });
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.ConfirmedAt);

            b.HasOne(x => x.User)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccessToken>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Token).IsUnique();
            b.Property(x => x.Token).HasMaxLength(128).IsRequired();
            b.Property(x => x.IsUsed).IsRequired().HasDefaultValue(false);
            b.Property(x => x.Expiry).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();

            b.HasOne(x => x.User)
                .WithMany(x => x.AccessTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
