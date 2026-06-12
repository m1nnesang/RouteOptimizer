using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities.Orders;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");


        builder.HasDiscriminator<string>("OrderType")
            .HasValue<BusinessOrder>("Business")
            .HasValue<IndividualOrder>("Individual");

        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.CargoType).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.AssignedRouteId);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.OwnsOne(x => x.Address);

        builder.OwnsOne(x => x.Location, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("Location_Latitude");
            loc.Property(l => l.Longitude).HasColumnName("Location_Longitude");
        });

        builder.OwnsOne(x => x.Weight, w => w.Property(p => p.Value).HasColumnName("WeightKg"));
        builder.OwnsOne(x => x.Volume, v => v.Property(p => p.Value).HasColumnName("VolumeM3"));

        builder.OwnsOne(x => x.Number, pn => pn.Property(p => p.Value).HasColumnName("PhoneNumber"));


        builder.OwnsOne(x => x.DeliveryWindow);

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Ignore(x => x.DomainEvents);
    }
}
