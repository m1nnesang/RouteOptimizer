using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities.Route;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("Routes");

        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.AssignedShiftId);
        builder.Property(x => x.Status).IsRequired();

        builder.HasMany(x => x.Stops)
            .WithOne()
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Stops).HasField("_stops");

        builder.Ignore(x => x.RemainingStops);
        builder.Ignore(x => x.CompletedStops);
        builder.Ignore(x => x.FailedStops);
        builder.Ignore(x => x.CurrentStop);

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Ignore(x => x.DomainEvents);
    }
}