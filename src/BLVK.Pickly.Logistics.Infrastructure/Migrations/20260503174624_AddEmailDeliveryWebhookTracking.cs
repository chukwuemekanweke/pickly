using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryWebhookTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnqueuedAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SentAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE notifications."EmailNotificationLogs"
                SET "EnqueuedAtUtc" = COALESCE("CreatedAtUtc", "UpdatedAtUtc"),
                    "SentAtUtc" = CASE
                        WHEN "IsSent" THEN COALESCE("UpdatedAtUtc", "CreatedAtUtc")
                        ELSE NULL
                    END;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EnqueuedAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "IsSent",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.CreateTable(
                name: "EmailDeliveryWebhookInboxes",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookEventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    SendingStream = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SendingDomainName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDeliveryWebhookInboxes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationLogs_ProviderMessageId",
                schema: "notifications",
                table: "EmailNotificationLogs",
                column: "ProviderMessageId",
                unique: true,
                filter: "\"ProviderMessageId\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryWebhookInboxes_ProviderId_WebhookEventId",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                columns: new[] { "ProviderId", "WebhookEventId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryWebhookInboxes_ProviderMessageId",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                column: "ProviderMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailDeliveryWebhookInboxes",
                schema: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_EmailNotificationLogs_ProviderMessageId",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "DeliveredAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "EnqueuedAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "SentAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.AddColumn<bool>(
                name: "IsSent",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
