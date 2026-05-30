using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasOne<User>().WithMany().HasForeignKey(x =>
            x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
