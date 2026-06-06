using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddMorePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeCorrespondents_Correspondents_CorrespondentId",
                table: "EpisodeCorrespondents");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_ScheduledExecutionTime",
                table: "Episodes");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeEmployees_EpisodeId",
                table: "EpisodeEmployees");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeCorrespondents_EpisodeId",
                table: "EpisodeCorrespondents");

            migrationBuilder.AlterColumn<string>(
                name: "Topic",
                table: "EpisodeCorrespondents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "EpisodeCorrespondents",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "EpisodeCorrespondents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_Active_FullName",
                table: "Guests",
                column: "FullName",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_ScheduledExecutionTime",
                table: "Episodes",
                column: "ScheduledExecutionTime",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UQ_EpisodeEmployees",
                table: "EpisodeEmployees",
                columns: new[] { "EpisodeId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_EpisodeCorrespondents",
                table: "EpisodeCorrespondents",
                columns: new[] { "EpisodeId", "CorrespondentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Active_FullName",
                table: "Employees",
                column: "FullName",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondents_Active_FullName",
                table: "Correspondents",
                column: "FullName",
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeCorrespondents_Correspondents_CorrespondentId",
                table: "EpisodeCorrespondents",
                column: "CorrespondentId",
                principalTable: "Correspondents",
                principalColumn: "CorrespondentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeCorrespondents_Correspondents_CorrespondentId",
                table: "EpisodeCorrespondents");

            migrationBuilder.DropIndex(
                name: "IX_Guests_Active_FullName",
                table: "Guests");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_ScheduledExecutionTime",
                table: "Episodes");

            migrationBuilder.DropIndex(
                name: "UQ_EpisodeEmployees",
                table: "EpisodeEmployees");

            migrationBuilder.DropIndex(
                name: "UQ_EpisodeCorrespondents",
                table: "EpisodeCorrespondents");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Active_FullName",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Correspondents_Active_FullName",
                table: "Correspondents");

            migrationBuilder.AlterColumn<string>(
                name: "Topic",
                table: "EpisodeCorrespondents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "EpisodeCorrespondents",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "EpisodeCorrespondents",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_ScheduledExecutionTime",
                table: "Episodes",
                column: "ScheduledExecutionTime");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeEmployees_EpisodeId",
                table: "EpisodeEmployees",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeCorrespondents_EpisodeId",
                table: "EpisodeCorrespondents",
                column: "EpisodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeCorrespondents_Correspondents_CorrespondentId",
                table: "EpisodeCorrespondents",
                column: "CorrespondentId",
                principalTable: "Correspondents",
                principalColumn: "CorrespondentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
