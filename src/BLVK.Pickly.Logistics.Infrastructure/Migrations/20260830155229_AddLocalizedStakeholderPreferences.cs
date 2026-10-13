using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizedStakeholderPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailNotificationTemplates_NotificationType",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "character varying(35)",
                maxLength: 35,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "system");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                type: "character varying(35)",
                maxLength: 35,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "character varying(35)",
                maxLength: 35,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationTemplates_NotificationType_Language",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                columns: new[] { "NotificationType", "Language" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailNotificationTemplates_NotificationType_Language",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.DropColumn(
                name: "Language",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "Theme",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "Language",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.DropColumn(
                name: "Language",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationTemplates_NotificationType",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                column: "NotificationType",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
