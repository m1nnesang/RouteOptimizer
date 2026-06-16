using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouteOptimizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropSpuriousStopForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stops_Routes_RouteId1",
                table: "Stops");

            migrationBuilder.DropForeignKey(
                name: "FK_Stops_Routes_RouteId2",
                table: "Stops");

            migrationBuilder.DropForeignKey(
                name: "FK_Stops_Routes_RouteId3",
                table: "Stops");

            migrationBuilder.DropIndex(
                name: "IX_Stops_RouteId1",
                table: "Stops");

            migrationBuilder.DropIndex(
                name: "IX_Stops_RouteId2",
                table: "Stops");

            migrationBuilder.DropIndex(
                name: "IX_Stops_RouteId3",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "RouteId1",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "RouteId2",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "RouteId3",
                table: "Stops");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RouteId1",
                table: "Stops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RouteId2",
                table: "Stops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RouteId3",
                table: "Stops",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stops_RouteId1",
                table: "Stops",
                column: "RouteId1");

            migrationBuilder.CreateIndex(
                name: "IX_Stops_RouteId2",
                table: "Stops",
                column: "RouteId2");

            migrationBuilder.CreateIndex(
                name: "IX_Stops_RouteId3",
                table: "Stops",
                column: "RouteId3");

            migrationBuilder.AddForeignKey(
                name: "FK_Stops_Routes_RouteId1",
                table: "Stops",
                column: "RouteId1",
                principalTable: "Routes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stops_Routes_RouteId2",
                table: "Stops",
                column: "RouteId2",
                principalTable: "Routes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stops_Routes_RouteId3",
                table: "Stops",
                column: "RouteId3",
                principalTable: "Routes",
                principalColumn: "Id");
        }
    }
}
