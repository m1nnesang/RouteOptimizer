using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class DeliveryAttemptConfiguration : IEntityTypeConfiguration<DeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<DeliveryAttempt> builder)
    {
        builder.ToTable("DeliveryAttempts");

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.DriverId).IsRequired();
        builder.Property(x => x.RouteId).IsRequired();
        builder.Property(x => x.AttemptedAt).IsRequired();
        builder.Property(x => x.Outcome).IsRequired();
        builder.Property(x => x.FailureReason);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.PhotoKey).HasMaxLength(200);

        builder.OwnsOne(x => x.AttemptLocation, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("Location_Latitude");
            loc.Property(l => l.Longitude).HasColumnName("Location_Longitude");
        });

        builder.Ignore(x => x.DomainEvents);
    }
}