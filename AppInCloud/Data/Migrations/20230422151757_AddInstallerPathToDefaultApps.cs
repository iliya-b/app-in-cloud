using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInCloud.Data.Migrations
{
    public partial class AddInstallerPathToDefaultApps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstallerPath",
                table: "DefaultApps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstallerPath",
                table: "DefaultApps");
        }
    }
}
