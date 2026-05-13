using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_CorrespondentCoverage_GuestId",
                table: "CorrespondentCoverage",
                newName: "IX_Coverages_GuestId");

            migrationBuilder.RenameIndex(
                name: "IX_CorrespondentCoverage_CorrespondentId",
                table: "CorrespondentCoverage",
                newName: "IX_Coverages_CorrespondentId");

            migrationBuilder.CreateIndex(
                name: "IX_WebsitePublishingLogs_PublishedAt",
                table: "WebsitePublishingLogs",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SocialMediaPublishingLogs_PublishedAt",
                table: "SocialMediaPublishingLogs",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Permissions_SystemName",
                table: "Permissions",
                column: "SystemName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_CreatedAt",
                table: "ExecutionLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_ScheduledExecutionTime",
                table: "Episodes",
                column: "ScheduledExecutionTime");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeGuests_EpisodeId",
                table: "EpisodeGuests",
                column: "EpisodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebsitePublishingLogs_PublishedAt",
                table: "WebsitePublishingLogs");

            migrationBuilder.DropIndex(
                name: "IX_SocialMediaPublishingLogs_PublishedAt",
                table: "SocialMediaPublishingLogs");

            migrationBuilder.DropIndex(
                name: "UQ_Roles_RoleName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UQ_Permissions_SystemName",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionLogs_CreatedAt",
                table: "ExecutionLogs");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_ScheduledExecutionTime",
                table: "Episodes");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeGuests_EpisodeId",
                table: "EpisodeGuests");

            migrationBuilder.RenameIndex(
                name: "IX_Coverages_GuestId",
                table: "CorrespondentCoverage",
                newName: "IX_CorrespondentCoverage_GuestId");

            migrationBuilder.RenameIndex(
                name: "IX_Coverages_CorrespondentId",
                table: "CorrespondentCoverage",
                newName: "IX_CorrespondentCoverage_CorrespondentId");
        }
    }
}
