﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInCloud.Data.Migrations
{
    public partial class AddStartedAtColumnToDevices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Devices");
        }
    }
}
