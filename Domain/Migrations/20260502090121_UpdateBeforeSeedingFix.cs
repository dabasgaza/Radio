using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBeforeSeedingFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EpisodeStatuses",
                keyColumn: "StatusId",
                keyValue: (byte)3,
                columns: new[] { "DisplayName", "StatusName" },
                values: new object[] { "منشورة على الموقع", "WebsitePublished" });

            migrationBuilder.InsertData(
                table: "EpisodeStatuses",
                columns: new[] { "StatusId", "DisplayName", "SortOrder", "StatusName" },
                values: new object[] { (byte)4, "ملغاة", (byte)4, "Cancelled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EpisodeStatuses",
                keyColumn: "StatusId",
                keyValue: (byte)4);

            migrationBuilder.UpdateData(
                table: "EpisodeStatuses",
                keyColumn: "StatusId",
                keyValue: (byte)3,
                columns: new[] { "DisplayName", "StatusName" },
                values: new object[] { "ملغاة", "Cancelled" });
        }
    }
}
