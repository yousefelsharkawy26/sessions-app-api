using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInactivityMutingAndLockouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MutedUntil",
                table: "GroupMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedRecoveryAttempts",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecoveryLockoutEnd",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DirectChatMutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MuterId = table.Column<string>(type: "text", nullable: false),
                    MutedUserId = table.Column<string>(type: "text", nullable: false),
                    MutedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectChatMutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectChatMutes_AspNetUsers_MutedUserId",
                        column: x => x.MutedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DirectChatMutes_AspNetUsers_MuterId",
                        column: x => x.MuterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DirectChatMutes_MutedUserId",
                table: "DirectChatMutes",
                column: "MutedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectChatMutes_MuterId_MutedUserId",
                table: "DirectChatMutes",
                columns: new[] { "MuterId", "MutedUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectChatMutes");

            migrationBuilder.DropColumn(
                name: "MutedUntil",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "FailedRecoveryAttempts",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RecoveryLockoutEnd",
                table: "AspNetUsers");
        }
    }
}
