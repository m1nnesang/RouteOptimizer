using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouteOptimizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressApartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address_Apartment",
                table: "Stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Apartment",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_Apartment",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "Address_Apartment",
                table: "Orders");
        }
    }
}
