using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class MoveStaffRoleToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeEmployees_StaffRoles_StaffRoleId",
                table: "EpisodeEmployees");

            migrationBuilder.AlterColumn<int>(
                name: "StaffRoleId",
                table: "EpisodeEmployees",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "StaffRoleId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_StaffRoleId",
                table: "Employees",
                column: "StaffRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_StaffRoles_StaffRoleId",
                table: "Employees",
                column: "StaffRoleId",
                principalTable: "StaffRoles",
                principalColumn: "StaffRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeEmployees_StaffRoles_StaffRoleId",
                table: "EpisodeEmployees",
                column: "StaffRoleId",
                principalTable: "StaffRoles",
                principalColumn: "StaffRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_StaffRoles_StaffRoleId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeEmployees_StaffRoles_StaffRoleId",
                table: "EpisodeEmployees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_StaffRoleId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "StaffRoleId",
                table: "Employees");

            migrationBuilder.AlterColumn<int>(
                name: "StaffRoleId",
                table: "EpisodeEmployees",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeEmployees_StaffRoles_StaffRoleId",
                table: "EpisodeEmployees",
                column: "StaffRoleId",
                principalTable: "StaffRoles",
                principalColumn: "StaffRoleId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
