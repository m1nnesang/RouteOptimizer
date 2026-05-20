using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteOptimizer.Domain.Entities.Orders;

namespace RouteOptimizer.Infrastructure.Persistence.Configurations;

public class IndividualOrderConfiguration : IEntityTypeConfiguration<IndividualOrder>
{
    public void Configure(EntityTypeBuilder<IndividualOrder> builder)
    {
        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.AllowLeaveAtDoor).IsRequired();
    }
}