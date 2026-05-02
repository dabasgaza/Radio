using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class ComprehensiveSchemaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublishingLogs");

            migrationBuilder.AddColumn<string>(
                name: "ClipNotes",
                table: "EpisodeGuests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ClipStatus",
                table: "EpisodeGuests",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_Employees_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeCorrespondents",
                columns: table => new
                {
                    EpisodeCorrespondentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<int>(type: "int", nullable: false),
                    CorrespondentId = table.Column<int>(type: "int", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HostingTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeCorrespondents", x => x.EpisodeCorrespondentId);
                    table.ForeignKey(
                        name: "FK_EpisodeCorrespondents_Correspondents_CorrespondentId",
                        column: x => x.CorrespondentId,
                        principalTable: "Correspondents",
                        principalColumn: "CorrespondentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeCorrespondents_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "EpisodeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeCorrespondents_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EpisodeCorrespondents_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SocialMediaPlatforms",
                columns: table => new
                {
                    SocialMediaPlatformId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialMediaPlatforms", x => x.SocialMediaPlatformId);
                    table.ForeignKey(
                        name: "FK_SocialMediaPlatforms_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialMediaPlatforms_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SocialMediaPublishingLogs",
                columns: table => new
                {
                    SocialMediaPublishingLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeGuestId = table.Column<int>(type: "int", nullable: false),
                    PublishedByUserId = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<byte>(type: "tinyint", nullable: false),
                    ClipDuration = table.Column<TimeSpan>(type: "time", nullable: true),
                    ClipTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EpisodeId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialMediaPublishingLogs", x => x.SocialMediaPublishingLogId);
                    table.ForeignKey(
                        name: "FK_SocialMediaPublishingLogs_EpisodeGuests_EpisodeGuestId",
                        column: x => x.EpisodeGuestId,
                        principalTable: "EpisodeGuests",
                        principalColumn: "EpisodeGuestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialMediaPublishingLogs_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "EpisodeId");
                    table.ForeignKey(
                        name: "FK_SocialMediaPublishingLogs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialMediaPublishingLogs_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialMediaPublishingLogs_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffRoles",
                columns: table => new
                {
                    StaffRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffRoles", x => x.StaffRoleId);
                    table.ForeignKey(
                        name: "FK_StaffRoles_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffRoles_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WebsitePublishingLogs",
                columns: table => new
                {
                    WebsitePublishingLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<int>(type: "int", nullable: false),
                    PublishedByUserId = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsitePublishingLogs", x => x.WebsitePublishingLogId);
                    table.ForeignKey(
                        name: "FK_WebsitePublishingLogs_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "EpisodeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WebsitePublishingLogs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WebsitePublishingLogs_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WebsitePublishingLogs_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SocialMediaPublishingLogPlatforms",
                columns: table => new
                {
                    SocialMediaPublishingLogPlatformId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SocialMediaPublishingLogId = table.Column<int>(type: "int", nullable: false),
                    SocialMediaPlatformId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialMediaPublishingLogPlatforms", x => x.SocialMediaPublishingLogPlatformId);
                    table.ForeignKey(
                        name: "FK_SocialMediaPublishingLogPlatforms_SocialMediaPlatforms_SocialMediaPlatformId",
                        column: x => x.SocialMediaPlatformId,
                        principalTable: "SocialMediaPlatforms",
                        principalColumn: "SocialMediaPlatformId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialMediaPublishingLogPlatforms_SocialMediaPublishingLogs_SocialMediaPublishingLogId",
                        column: x => x.SocialMediaPublishingLogId,
                        principalTable: "SocialMediaPublishingLogs",
                        principalColumn: "SocialMediaPublishingLogId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialMediaPublishingLogPlatforms_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialMediaPublishingLogPlatforms_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeEmployees",
                columns: table => new
                {
                    EpisodeEmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StaffRoleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeEmployees", x => x.EpisodeEmployeeId);
                    table.ForeignKey(
                        name: "FK_EpisodeEmployees_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeEmployees_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "EpisodeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeEmployees_StaffRoles_StaffRoleId",
                        column: x => x.StaffRoleId,
                        principalTable: "StaffRoles",
                        principalColumn: "StaffRoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeEmployees_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EpisodeEmployees_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CreatedByUserId",
                table: "Employees",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UpdatedByUserId",
                table: "Employees",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeCorrespondents_CorrespondentId",
                table: "EpisodeCorrespondents",
                column: "CorrespondentId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeCorrespondents_CreatedByUserId",
                table: "EpisodeCorrespondents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeCorrespondents_EpisodeId",
                table: "EpisodeCorrespondents",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeCorrespondents_UpdatedByUserId",
                table: "EpisodeCorrespondents",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeEmployees_CreatedByUserId",
                table: "EpisodeEmployees",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeEmployees_EmployeeId",
                table: "EpisodeEmployees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeEmployees_EpisodeId",
                table: "EpisodeEmployees",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeEmployees_StaffRoleId",
                table: "EpisodeEmployees",
                column: "StaffRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeEmployees_UpdatedByUserId",
                table: "EpisodeEmployees",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPlatforms_CreatedByUserId",
                table: "SocialMediaPlatforms",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPlatforms_UpdatedByUserId",
                table: "SocialMediaPlatforms",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogPlatforms_CreatedByUserId",
                table: "SocialMediaPublishingLogPlatforms",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogPlatforms_SocialMediaPlatformId",
                table: "SocialMediaPublishingLogPlatforms",
                column: "SocialMediaPlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogPlatforms_SocialMediaPublishingLogId",
                table: "SocialMediaPublishingLogPlatforms",
                column: "SocialMediaPublishingLogId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogPlatforms_UpdatedByUserId",
                table: "SocialMediaPublishingLogPlatforms",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogs_CreatedByUserId",
                table: "SocialMediaPublishingLogs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogs_EpisodeGuestId",
                table: "SocialMediaPublishingLogs",
                column: "EpisodeGuestId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogs_EpisodeId",
                table: "SocialMediaPublishingLogs",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogs_PublishedByUserId",
                table: "SocialMediaPublishingLogs",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogs_UpdatedByUserId",
                table: "SocialMediaPublishingLogs",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRoles_CreatedByUserId",
                table: "StaffRoles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRoles_UpdatedByUserId",
                table: "StaffRoles",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebsitePublishingLogs_CreatedByUserId",
                table: "WebsitePublishingLogs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebsitePublishingLogs_EpisodeId",
                table: "WebsitePublishingLogs",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WebsitePublishingLogs_PublishedByUserId",
                table: "WebsitePublishingLogs",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebsitePublishingLogs_UpdatedByUserId",
                table: "WebsitePublishingLogs",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodeCorrespondents");

            migrationBuilder.DropTable(
                name: "EpisodeEmployees");

            migrationBuilder.DropTable(
                name: "SocialMediaPublishingLogPlatforms");

            migrationBuilder.DropTable(
                name: "WebsitePublishingLogs");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "StaffRoles");

            migrationBuilder.DropTable(
                name: "SocialMediaPlatforms");

            migrationBuilder.DropTable(
                name: "SocialMediaPublishingLogs");

            migrationBuilder.DropColumn(
                name: "ClipNotes",
                table: "EpisodeGuests");

            migrationBuilder.DropColumn(
                name: "ClipStatus",
                table: "EpisodeGuests");

            migrationBuilder.CreateTable(
                name: "PublishingLogs",
                columns: table => new
                {
                    PublishingLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<int>(type: "int", nullable: false),
                    PublishedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FacebookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SoundCloudUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TwitterUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    YouTubeUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishingLogs", x => x.PublishingLogId);
                    table.ForeignKey(
                        name: "FK_PublishingLogs_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "EpisodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublishingLogs_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublishingLogs_EpisodeId",
                table: "PublishingLogs",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PublishingLogs_PublishedByUserId",
                table: "PublishingLogs",
                column: "PublishedByUserId");
        }
    }
}
