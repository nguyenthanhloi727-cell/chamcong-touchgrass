using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLastLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LastLat",
                table: "Users",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastLng",
                table: "Users",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLocationAt",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLat",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLng",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLocationAt",
                table: "Users");
        }
    }
}
