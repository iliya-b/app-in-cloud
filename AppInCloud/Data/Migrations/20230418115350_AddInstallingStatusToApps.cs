using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInCloud.Data.Migrations
{
    public partial class AddInstallingStatusToApps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MobileApps_DeviceId",
                table: "MobileApps",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MobileApps_Devices_DeviceId",
                table: "MobileApps",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MobileApps_Devices_DeviceId",
                table: "MobileApps");

            migrationBuilder.DropIndex(
                name: "IX_MobileApps_DeviceId",
                table: "MobileApps");
        }
    }
}
