using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInCloud.Data.Migrations
{
    public partial class AddActivityInformationToDevices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Target",
                table: "Devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Target",
                table: "Devices");
        }
    }
}
