using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class DriverShiftConfiguration : IEntityTypeConfiguration<DriverShift>
{
    public void Configure(EntityTypeBuilder<DriverShift> builder)
    {
        builder.ToTable("DriverShifts");

        builder.Property(x => x.DriverId).IsRequired();
        builder.Property(x => x.VehicleId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.ShiftDate).IsRequired();
        builder.Property(x => x.StartedAt);
        builder.Property(x => x.EndedAt);

        builder.Ignore(x => x.DomainEvents);
    }
}