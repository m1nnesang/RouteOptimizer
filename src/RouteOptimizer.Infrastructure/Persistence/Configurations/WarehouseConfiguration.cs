using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.OwnsOne(x => x.Address, a =>
        {
            a.Property(p => p.Street).HasColumnName("Address_Street");
            a.Property(p => p.City).HasColumnName("Address_City");
            a.Property(p => p.PostalCode).HasColumnName("Address_PostalCode");
            a.Property(p => p.Country).HasColumnName("Address_Country");
        });
        builder.OwnsOne(x => x.Location, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("Latitude");
            loc.Property(l => l.Longitude).HasColumnName("Longitude");
        });
    }
}