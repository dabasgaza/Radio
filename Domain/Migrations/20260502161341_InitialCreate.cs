using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecordId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeStatuses",
                columns: table => new
                {
                    StatusId = table.Column<byte>(type: "tinyint", nullable: false),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeStatuses", x => x.StatusId);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Correspondents",
                columns: table => new
                {
                    CorrespondentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AssignedLocations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Correspondents", x => x.CorrespondentId);
                    table.ForeignKey(
                        name: "FK_Correspondents_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Correspondents_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

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
                name: "Guests",
                columns: table => new
                {
                    GuestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guests", x => x.GuestId);
                    table.ForeignKey(
                        name: "FK_Guests_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Guests_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    ProgramId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProgramDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.ProgramId);
                    table.ForeignKey(
                        name: "FK_Programs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Programs_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
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
                name: "CorrespondentCoverage",
                columns: table => new
                {
                    CoverageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CorrespondentId = table.Column<int>(type: "int", nullable: false),
                    GuestId = table.Column<int>(type: "int", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Topic = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScheduledTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrespondentCoverage", x => x.CoverageId);
                    table.ForeignKey(
                        name: "FK_CorrespondentCoverage_Correspondents_CorrespondentId",
                        column: x => x.CorrespondentId,
                        principalTable: "Correspondents",
                        principalColumn: "CorrespondentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrespondentCoverage_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "GuestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrespondentCoverage_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_CorrespondentCoverage_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Episodes",
                columns: table => new
                {
                    EpisodeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    EpisodeName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EpisodeDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ScheduledExecutionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualExecutionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SpecialNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StatusId = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.EpisodeId);
                    table.ForeignKey(
                        name: "FK_Episodes_EpisodeStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "EpisodeStatuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Episodes_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "ProgramId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Episodes_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Episodes_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
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

            migrationBuilder.CreateTable(
                name: "EpisodeGuests",
                columns: table => new
                {
                    EpisodeGuestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<int>(type: "int", nullable: false),
                    GuestId = table.Column<int>(type: "int", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HostingTime = table.Column<TimeSpan>(type: "TIME", nullable: true),
                    ClipStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ClipNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeGuests", x => x.EpisodeGuestId);
                    table.ForeignKey(
                        name: "FK_EpisodeGuests_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "EpisodeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeGuests_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "GuestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeGuests_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_EpisodeGuests_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ExecutionLogs",
                columns: table => new
                {
                    ExecutionLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<int>(type: "int", nullable: false),
                    ExecutedByUserId = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ExecutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IssuesEncountered = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionLogs", x => x.ExecutionLogId);
                    table.ForeignKey(
                        name: "FK_ExecutionLogs_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "EpisodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionLogs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionLogs_Users_ExecutedByUserId",
                        column: x => x.ExecutedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionLogs_Users_UpdatedByUserId",
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

            migrationBuilder.InsertData(
                table: "EpisodeStatuses",
                columns: new[] { "StatusId", "DisplayName", "SortOrder", "StatusName" },
                values: new object[,]
                {
                    { (byte)0, "مجدولة", (byte)0, "Planned" },
                    { (byte)1, "منفّذة", (byte)1, "Executed" },
                    { (byte)2, "منشورة", (byte)2, "Published" },
                    { (byte)3, "منشورة على الموقع", (byte)3, "WebsitePublished" },
                    { (byte)4, "ملغاة", (byte)4, "Cancelled" }
                });

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
                    { 11, "عرض التقارير", "التقارير", "VIEW_REPORTS" },
                    { 12, "تراجع عن تنفيذ/نشر", "الحلقات", "EPISODE_REVERT" }
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
                    { 12, 1 },
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

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ChangedAt",
                table: "AuditLogs",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_RecordId",
                table: "AuditLogs",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Table_Record",
                table: "AuditLogs",
                columns: new[] { "TableName", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_TableName",
                table: "AuditLogs",
                column: "TableName");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondentCoverage_CorrespondentId",
                table: "CorrespondentCoverage",
                column: "CorrespondentId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondentCoverage_CreatedByUserId",
                table: "CorrespondentCoverage",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondentCoverage_GuestId",
                table: "CorrespondentCoverage",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondentCoverage_UpdatedByUserId",
                table: "CorrespondentCoverage",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondents_CreatedByUserId",
                table: "Correspondents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondents_UpdatedByUserId",
                table: "Correspondents",
                column: "UpdatedByUserId");

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
                name: "IX_EpisodeGuests_CreatedByUserId",
                table: "EpisodeGuests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeGuests_GuestId",
                table: "EpisodeGuests",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeGuests_UpdatedByUserId",
                table: "EpisodeGuests",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_EpisodeGuests",
                table: "EpisodeGuests",
                columns: new[] { "EpisodeId", "GuestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_CreatedByUserId",
                table: "Episodes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_ProgramId",
                table: "Episodes",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_StatusId",
                table: "Episodes",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_UpdatedByUserId",
                table: "Episodes",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_CreatedByUserId",
                table: "ExecutionLogs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_EpisodeId",
                table: "ExecutionLogs",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_ExecutedByUserId",
                table: "ExecutionLogs",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_UpdatedByUserId",
                table: "ExecutionLogs",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_CreatedByUserId",
                table: "Guests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_UpdatedByUserId",
                table: "Guests",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Programs_CreatedByUserId",
                table: "Programs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Programs_UpdatedByUserId",
                table: "Programs",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Programs_ProgramName",
                table: "Programs",
                column: "ProgramName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

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
                name: "IX_Users_CreatedByUserId",
                table: "Users",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedByUserId",
                table: "Users",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

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
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CorrespondentCoverage");

            migrationBuilder.DropTable(
                name: "EpisodeCorrespondents");

            migrationBuilder.DropTable(
                name: "EpisodeEmployees");

            migrationBuilder.DropTable(
                name: "ExecutionLogs");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SocialMediaPublishingLogPlatforms");

            migrationBuilder.DropTable(
                name: "WebsitePublishingLogs");

            migrationBuilder.DropTable(
                name: "Correspondents");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "StaffRoles");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "SocialMediaPlatforms");

            migrationBuilder.DropTable(
                name: "SocialMediaPublishingLogs");

            migrationBuilder.DropTable(
                name: "EpisodeGuests");

            migrationBuilder.DropTable(
                name: "Episodes");

            migrationBuilder.DropTable(
                name: "Guests");

            migrationBuilder.DropTable(
                name: "EpisodeStatuses");

            migrationBuilder.DropTable(
                name: "Programs");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
