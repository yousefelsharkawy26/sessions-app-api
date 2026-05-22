using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddE2eeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Content",
                table: "Messages",
                newName: "Ciphertext");

            migrationBuilder.AddColumn<string>(
                name: "EphemeralKey",
                table: "Messages",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OneTimePrekeyIdUsed",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignedPrekeyIdUsed",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OneTimePrekeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    KeyId = table.Column<int>(type: "integer", nullable: false),
                    KeyData = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimePrekeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimePrekeys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrekeyBundles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    IdentityKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SignedPrekey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Signature = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SignedPrekeyId = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrekeyBundles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrekeyBundles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OneTimePrekeys_UserId",
                table: "OneTimePrekeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimePrekeys_UserId_KeyId",
                table: "OneTimePrekeys",
                columns: new[] { "UserId", "KeyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrekeyBundles_UserId",
                table: "PrekeyBundles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneTimePrekeys");

            migrationBuilder.DropTable(
                name: "PrekeyBundles");

            migrationBuilder.DropColumn(
                name: "EphemeralKey",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "OneTimePrekeyIdUsed",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SignedPrekeyIdUsed",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "Ciphertext",
                table: "Messages",
                newName: "Content");
        }
    }
}
