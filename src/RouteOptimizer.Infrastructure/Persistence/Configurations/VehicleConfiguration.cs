using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasIndex(x => x.WarehouseId);
        builder.Property(x => x.Type).HasMaxLength(20).IsRequired();
        builder.OwnsOne(x => x.MaxWeightKg, w => w.Property(p => p.Value).HasColumnName("MaxWeightKg"));
        builder.OwnsOne(x => x.MaxVolumeM3, v => v.Property(p => p.Value).HasColumnName("MaxVolumeM3"));

        builder.Property(x => x.AllowedCargoTypes)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<CargoType>>(v, JsonSerializerOptions.Default)!
            )
            .IsRequired();

        builder.Property(x => x.LicenseCategory).IsRequired();
        builder.Ignore(x => x.DomainEvents);
    }
}
