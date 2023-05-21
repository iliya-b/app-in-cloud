using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInCloud.Data.Migrations
{
    public partial class AddMachineRelatedLimitsToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AllowedMachineAmount",
                table: "AspNetUsers",
                newName: "AllowedRunningMachinesAmount");

            migrationBuilder.AddColumn<int>(
                name: "AllowedMachinesAmount",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DailyLimit",
                table: "AspNetUsers",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "MonthlyLimit",
                table: "AspNetUsers",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedMachinesAmount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DailyLimit",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MonthlyLimit",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "AllowedRunningMachinesAmount",
                table: "AspNetUsers",
                newName: "AllowedMachineAmount");
        }
    }
}
