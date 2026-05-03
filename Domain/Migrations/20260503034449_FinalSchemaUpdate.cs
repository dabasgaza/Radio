using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class FinalSchemaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeEmployees_StaffRoles_StaffRoleId",
                table: "EpisodeEmployees");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeEmployees_StaffRoleId",
                table: "EpisodeEmployees");

            migrationBuilder.DropColumn(
                name: "StaffRoleId",
                table: "EpisodeEmployees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StaffRoleId",
                table: "EpisodeEmployees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeEmployees_StaffRoleId",
                table: "EpisodeEmployees",
                column: "StaffRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeEmployees_StaffRoles_StaffRoleId",
                table: "EpisodeEmployees",
                column: "StaffRoleId",
                principalTable: "StaffRoles",
                principalColumn: "StaffRoleId");
        }
    }
}
