using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageDeliveryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Messages");
        }
    }
}
