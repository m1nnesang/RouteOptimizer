using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouteOptimizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Routes",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "Routes");
        }
    }
}
