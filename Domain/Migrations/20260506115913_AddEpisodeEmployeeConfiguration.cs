using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddEpisodeEmployeeConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeEmployees_Employees_EmployeeId",
                table: "EpisodeEmployees");

            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeEmployees_StaffRoles_StaffRoleId",
                table: "EpisodeEmployees");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeEmployees_StaffRoleId",
                table: "EpisodeEmployees");

            migrationBuilder.DropColumn(
                name: "StaffRoleId",
                table: "EpisodeEmployees");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "EpisodeEmployees",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "EpisodeEmployees",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeEmployees_Employees_EmployeeId",
                table: "EpisodeEmployees",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeEmployees_Employees_EmployeeId",
                table: "EpisodeEmployees");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "EpisodeEmployees",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "EpisodeEmployees",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

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
                name: "FK_EpisodeEmployees_Employees_EmployeeId",
                table: "EpisodeEmployees",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeEmployees_StaffRoles_StaffRoleId",
                table: "EpisodeEmployees",
                column: "StaffRoleId",
                principalTable: "StaffRoles",
                principalColumn: "StaffRoleId");
        }
    }
}
