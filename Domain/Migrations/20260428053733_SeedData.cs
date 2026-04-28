using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "DisplayName", "Module", "SystemName" },
                values: new object[,]
                {
                    { 1, "إدارة المستخدمين", "المستخدمين", "USER_MANAGE" },
                    { 2, "إدارة البرامج", "البرامج", "PROGRAM_MANAGE" },
                    { 3, "إدارة الحلقات", "الحلقات", "EPISODE_MANAGE" },
                    { 4, "تنفيذ الحلقات", "الحلقات", "EPISODE_EXECUTE" },
                    { 5, "نشر رقمي", "الحلقات", "EPISODE_PUBLISH" },
                    { 6, "نشر الموقع", "الحلقات", "EPISODE_WEB_PUBLISH" },
                    { 7, "تعديل الحلقات", "الحلقات", "EPISODE_EDIT" },
                    { 8, "حذف الحلقات", "الحلقات", "EPISODE_DELETE" },
                    { 9, "إدارة الضيوف", "الضيوف", "GUEST_MANAGE" },
                    { 10, "إدارة التنسيق الميداني", "التنسيق", "CORR_MANAGE" },
                    { 11, "عرض التقارير", "التقارير", "VIEW_REPORTS" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "CreatedAt", "IsActive", "RoleDescription", "RoleName", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), true, "مسؤول النظام — صلاحيات كاملة", "Admin", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), true, "مدير البرامج", "ProgramMgr", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), true, "مخرج البث", "Director", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), true, "ناشر الموقع", "WebPublisher", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 1 },
                    { 8, 1 },
                    { 9, 1 },
                    { 10, 1 },
                    { 11, 1 },
                    { 2, 2 },
                    { 3, 2 },
                    { 4, 2 },
                    { 5, 2 },
                    { 7, 2 },
                    { 9, 2 },
                    { 11, 2 },
                    { 4, 3 },
                    { 11, 3 },
                    { 6, 4 },
                    { 11, 4 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "CreatedByUserId", "EmailAddress", "FullName", "IsActive", "LastLoginAt", "PasswordHash", "PhoneNumber", "RoleId", "UpdatedAt", "UpdatedByUserId", "Username" },
                values: new object[] { 1, new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "مدير النظام", true, null, "$2a$11$24Mf/Vktd2tHGnC3f/iyTOmKMaQtcy4T0qOT07h22jC0Teor66hZa", null, 1, new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin" });

            migrationBuilder.InsertData(
                table: "Correspondents",
                columns: new[] { "CorrespondentId", "AssignedLocations", "CreatedAt", "CreatedByUserId", "FullName", "IsActive", "PhoneNumber", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, "الرياض", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, "محمد الحربي", true, "0550000001", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, "جدة", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, "فهد المطيري", true, "0550000002", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3, "الدمام", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, "عبدالله العنزي", true, "0550000003", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "Guests",
                columns: new[] { "GuestId", "CreatedAt", "CreatedByUserId", "EmailAddress", "FullName", "IsActive", "Organization", "PhoneNumber", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, "ahmed@example.com", "د. أحمد العمري", true, "جامعة الملك سعود", "0500000001", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, "sara@example.com", "أ. سارة القحطاني", true, "وزارة الثقافة", "0500000002", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3, new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, "khalid@example.com", "م. خالد الشهري", true, "هيئة الرياضة", "0500000003", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "Programs",
                columns: new[] { "ProgramId", "Category", "CreatedAt", "CreatedByUserId", "IsActive", "ProgramDescription", "ProgramName", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, "أخبار", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "النشرة الإخبارية اليومية", "نشرة الأخبار", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, "منوعات", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "برنامج صباحي منوع", "صباح الخير", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3, "رياضة", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "تحليل ونقاش رياضي", "حديث الرياضة", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 4, "ثقافة", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "برنامج ثقافي أدبي", "نافذة ثقافية", new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Correspondents",
                keyColumn: "CorrespondentId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Correspondents",
                keyColumn: "CorrespondentId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Correspondents",
                keyColumn: "CorrespondentId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Guests",
                keyColumn: "GuestId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Guests",
                keyColumn: "GuestId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Guests",
                keyColumn: "GuestId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "ProgramId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "ProgramId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "ProgramId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "ProgramId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 3 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 3 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 4 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 4 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1);
        }
    }
}
