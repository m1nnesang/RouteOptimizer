using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouteOptimizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressAndDeliveryWindowColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address_City",
                table: "Warehouses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Country",
                table: "Warehouses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_PostalCode",
                table: "Warehouses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Street",
                table: "Warehouses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_City",
                table: "Stops",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Country",
                table: "Stops",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_PostalCode",
                table: "Stops",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Street",
                table: "Stops",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DeliveryWindow_End",
                table: "Stops",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DeliveryWindow_Start",
                table: "Stops",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryWindow_Strictness",
                table: "Stops",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DeliveryWindow_Tolerance",
                table: "Stops",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_City",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Country",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_PostalCode",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Street",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DeliveryWindow_End",
                table: "Orders",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DeliveryWindow_Start",
                table: "Orders",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryWindow_Strictness",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DeliveryWindow_Tolerance",
                table: "Orders",
                type: "interval",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_City",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Address_Country",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Address_PostalCode",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Address_Street",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Address_City",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "Address_Country",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "Address_PostalCode",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "Address_Street",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "DeliveryWindow_End",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "DeliveryWindow_Start",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "DeliveryWindow_Strictness",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "DeliveryWindow_Tolerance",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "Address_City",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address_Country",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address_PostalCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address_Street",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryWindow_End",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryWindow_Start",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryWindow_Strictness",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryWindow_Tolerance",
                table: "Orders");
        }
    }
}
