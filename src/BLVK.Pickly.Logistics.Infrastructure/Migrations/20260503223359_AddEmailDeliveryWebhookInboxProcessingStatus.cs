using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryWebhookInboxProcessingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedAtUtc",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingError",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusChangeReason",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WebhookProcessingStatus",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryWebhookInboxes_WebhookProcessingStatus",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                column: "WebhookProcessingStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailDeliveryWebhookInboxes_WebhookProcessingStatus",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "ProcessedAtUtc",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "ProcessingError",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "StatusChangeReason",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "WebhookProcessingStatus",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes");
        }
    }
}
