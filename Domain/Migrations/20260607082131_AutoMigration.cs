using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class AutoMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Correspondents",
                keyColumn: "CorrespondentId",
                keyValue: 1,
                columns: new[] { "AssignedLocations", "FullName" },
                values: new object[] { "الجنوب", "مثنى النجار" });

            migrationBuilder.UpdateData(
                table: "Correspondents",
                keyColumn: "CorrespondentId",
                keyValue: 2,
                columns: new[] { "AssignedLocations", "FullName" },
                values: new object[] { "الشمال", "محمد أبو مرسة" });

            migrationBuilder.UpdateData(
                table: "Correspondents",
                keyColumn: "CorrespondentId",
                keyValue: 3,
                columns: new[] { "AssignedLocations", "FullName" },
                values: new object[] { "غزة", "خميس أبو حصيرة" });

            migrationBuilder.UpdateData(
                table: "Guests",
                keyColumn: "GuestId",
                keyValue: 1,
                columns: new[] { "FullName", "Organization" },
                values: new object[] { "صائب عريقات", "منظمة التحرير" });

            migrationBuilder.UpdateData(
                table: "Guests",
                keyColumn: "GuestId",
                keyValue: 2,
                columns: new[] { "FullName", "Organization" },
                values: new object[] { "يحيى السراج", "بلدية غزة" });

            migrationBuilder.UpdateData(
                table: "Guests",
                keyColumn: "GuestId",
                keyValue: 3,
                column: "FullName",
                value: "م. خالد");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Correspondents",
                keyColumn: "CorrespondentId",
                keyValue: 1,
                columns: new[] { "AssignedLocations", "FullName" },
                values: new object[] { "الرياض", "محمد الحربي" });

            migrationBuilder.UpdateData(
                table: "Correspondents",
                keyColumn: "CorrespondentId",
                keyValue: 2,
                columns: new[] { "AssignedLocations", "FullName" },
                values: new object[] { "جدة", "فهد المطيري" });

            migrationBuilder.UpdateData(
                table: "Correspondents",
                keyColumn: "CorrespondentId",
                keyValue: 3,
                columns: new[] { "AssignedLocations", "FullName" },
                values: new object[] { "الدمام", "عبدالله العنزي" });

            migrationBuilder.UpdateData(
                table: "Guests",
                keyColumn: "GuestId",
                keyValue: 1,
                columns: new[] { "FullName", "Organization" },
                values: new object[] { "د. أحمد العمري", "جامعة الملك سعود" });

            migrationBuilder.UpdateData(
                table: "Guests",
                keyColumn: "GuestId",
                keyValue: 2,
                columns: new[] { "FullName", "Organization" },
                values: new object[] { "أ. سارة القحطاني", "وزارة الثقافة" });

            migrationBuilder.UpdateData(
                table: "Guests",
                keyColumn: "GuestId",
                keyValue: 3,
                column: "FullName",
                value: "م. خالد الشهري");
        }
    }
}
