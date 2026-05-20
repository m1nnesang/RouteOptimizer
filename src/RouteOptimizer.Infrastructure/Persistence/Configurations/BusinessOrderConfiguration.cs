using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities.Orders;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class BusinessOrderConfiguration : IEntityTypeConfiguration<BusinessOrder>
{
    public void Configure(EntityTypeBuilder<BusinessOrder> builder)
    {
        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ContactPerson).HasMaxLength(100);
        builder.Property(x => x.RequiresSignature).IsRequired();
    }
}