using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricer.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettingsState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OperatingCurrencyId",
                table: "Settings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedPrinterId",
                table: "Settings",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperatingCurrencyId",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SelectedPrinterId",
                table: "Settings");
        }
    }
}
