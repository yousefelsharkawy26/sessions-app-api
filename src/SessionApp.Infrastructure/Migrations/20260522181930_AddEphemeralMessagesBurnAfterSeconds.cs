using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEphemeralMessagesBurnAfterSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BurnAfterSeconds",
                table: "Messages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BurnAfterSeconds",
                table: "Messages");
        }
    }
}
