using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Token).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.UsedAt);

        builder.HasIndex(x => x.Token).IsUnique();

        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsUsed);

        builder.Ignore(x => x.DomainEvents);
    }
}
