using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouteOptimizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAttemptPhotoKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoKey",
                table: "DeliveryAttempts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoKey",
                table: "DeliveryAttempts");
        }
    }
}
