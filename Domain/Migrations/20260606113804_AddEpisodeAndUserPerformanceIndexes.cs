using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddEpisodeAndUserPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive_FullName",
                table: "Users",
                columns: new[] { "IsActive", "FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_EpisodeName",
                table: "Episodes",
                column: "EpisodeName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive_FullName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_EpisodeName",
                table: "Episodes");
        }
    }
}
