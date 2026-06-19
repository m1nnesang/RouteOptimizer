using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities.Route;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class StopConfiguration : IEntityTypeConfiguration<Stop>
{
    public void Configure(EntityTypeBuilder<Stop> builder)
    {
        builder.ToTable("Stops");

        builder.Property(x => x.RouteId).IsRequired();
        builder.Property(x => x.Sequence).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.OwnsOne(x => x.Address, a =>
        {
            a.Property(p => p.Street).HasColumnName("Address_Street");
            a.Property(p => p.City).HasColumnName("Address_City");
            a.Property(p => p.PostalCode).HasColumnName("Address_PostalCode");
            a.Property(p => p.Country).HasColumnName("Address_Country");
            a.Property(p => p.Apartment).HasColumnName("Address_Apartment");
        });

        builder.OwnsOne(x => x.Location, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("Location_Latitude");
            loc.Property(l => l.Longitude).HasColumnName("Location_Longitude");
        });

        builder.OwnsOne(x => x.DeliveryWindow, dw =>
        {
            dw.Property(p => p.Start).HasColumnName("DeliveryWindow_Start");
            dw.Property(p => p.End).HasColumnName("DeliveryWindow_End");
            dw.Property(p => p.Strictness).HasColumnName("DeliveryWindow_Strictness");
            dw.Property(p => p.Tolerance).HasColumnName("DeliveryWindow_Tolerance");
        });

        builder.Property(x => x.Orders)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)!
            )
            .IsRequired();

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Ignore(x => x.DomainEvents);
    }
}
