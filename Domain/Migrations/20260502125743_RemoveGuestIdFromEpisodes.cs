using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGuestIdFromEpisodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Episodes_Guests_GuestId",
                table: "Episodes");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_GuestId",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "GuestId",
                table: "Episodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GuestId",
                table: "Episodes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_GuestId",
                table: "Episodes",
                column: "GuestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Episodes_Guests_GuestId",
                table: "Episodes",
                column: "GuestId",
                principalTable: "Guests",
                principalColumn: "GuestId");
        }
    }
}
