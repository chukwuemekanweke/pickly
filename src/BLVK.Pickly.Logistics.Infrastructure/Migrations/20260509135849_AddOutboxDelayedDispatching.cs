using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDelayedDispatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_SentAtUtc_EnqueuedAtUtc",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "payments",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AvailableAtUtc",
                schema: "integration",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql(
                """
                UPDATE integration."OutboxMessages"
                SET "AvailableAtUtc" = "EnqueuedAtUtc"
                WHERE "AvailableAtUtc" = TIMESTAMPTZ '0001-01-01 00:00:00+00';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_SentAtUtc_AvailableAtUtc",
                schema: "integration",
                table: "OutboxMessages",
                columns: new[] { "SentAtUtc", "AvailableAtUtc" });

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION integration.notify_outbox_message_changed()
                RETURNS trigger
                AS $$
                BEGIN
                    PERFORM pg_notify('integration_outbox_changed', NEW."Id"::text);
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_outbox_messages_notify ON integration."OutboxMessages";
                CREATE TRIGGER trg_outbox_messages_notify
                AFTER INSERT OR UPDATE OF "AvailableAtUtc"
                ON integration."OutboxMessages"
                FOR EACH ROW
                WHEN (NEW."SentAtUtc" IS NULL)
                EXECUTE FUNCTION integration.notify_outbox_message_changed();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_outbox_messages_notify ON integration."OutboxMessages";
                DROP FUNCTION IF EXISTS integration.notify_outbox_message_changed();
                """);

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_SentAtUtc_AvailableAtUtc",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AvailableAtUtc",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<long>(
                name: "RowVersion",
                schema: "payments",
                table: "Wallets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "RowVersion",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "RowVersion",
                schema: "payments",
                table: "PaymentTransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_SentAtUtc_EnqueuedAtUtc",
                schema: "integration",
                table: "OutboxMessages",
                columns: new[] { "SentAtUtc", "EnqueuedAtUtc" });
        }
    }
}
