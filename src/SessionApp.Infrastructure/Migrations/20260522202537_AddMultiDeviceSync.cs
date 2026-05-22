using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiDeviceSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OneTimePrekeys_UserId_KeyId",
                table: "OneTimePrekeys");

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "OneTimePrekeys",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientDeviceId",
                table: "Messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdentityKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SignedPrekey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Signature = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SignedPrekeyId = table.Column<int>(type: "integer", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OneTimePrekeys_UserId_DeviceId_KeyId",
                table: "OneTimePrekeys",
                columns: new[] { "UserId", "DeviceId", "KeyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_RecipientDeviceId",
                table: "Messages",
                column: "RecipientDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId_DeviceId",
                table: "UserDevices",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDevices");

            migrationBuilder.DropIndex(
                name: "IX_OneTimePrekeys_UserId_DeviceId_KeyId",
                table: "OneTimePrekeys");

            migrationBuilder.DropIndex(
                name: "IX_Messages_RecipientDeviceId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "OneTimePrekeys");

            migrationBuilder.DropColumn(
                name: "RecipientDeviceId",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimePrekeys_UserId_KeyId",
                table: "OneTimePrekeys",
                columns: new[] { "UserId", "KeyId" },
                unique: true);
        }
    }
}
