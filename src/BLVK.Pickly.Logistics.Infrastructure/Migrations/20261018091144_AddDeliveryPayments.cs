using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProviderVerifiedAtUtc",
                schema: "payments",
                table: "PaymentTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VerifiedAmount",
                schema: "payments",
                table: "PaymentTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedCurrencyId",
                schema: "payments",
                table: "PaymentTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActivePaymentTransactionId",
                schema: "deliveries",
                table: "Deliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryPaymentAttempts",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AppliedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApplicationException = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_DeliveryPaymentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryPaymentAttempts_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "deliveries",
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryPaymentAttempts_PaymentTransactions_PaymentTransact~",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "payments",
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_ActivePaymentTransactionId",
                schema: "deliveries",
                table: "Deliveries",
                column: "ActivePaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPaymentAttempts_DeliveryId",
                schema: "deliveries",
                table: "DeliveryPaymentAttempts",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPaymentAttempts_DeliveryId_IdempotencyKey",
                schema: "deliveries",
                table: "DeliveryPaymentAttempts",
                columns: new[] { "DeliveryId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPaymentAttempts_PaymentTransactionId",
                schema: "deliveries",
                table: "DeliveryPaymentAttempts",
                column: "PaymentTransactionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_PaymentTransactions_ActivePaymentTransactionId",
                schema: "deliveries",
                table: "Deliveries",
                column: "ActivePaymentTransactionId",
                principalSchema: "payments",
                principalTable: "PaymentTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_PaymentTransactions_PaymentTransactionId",
                schema: "deliveries",
                table: "Deliveries",
                column: "PaymentTransactionId",
                principalSchema: "payments",
                principalTable: "PaymentTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_PaymentTransactions_ActivePaymentTransactionId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_PaymentTransactions_PaymentTransactionId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropTable(
                name: "DeliveryPaymentAttempts",
                schema: "deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_ActivePaymentTransactionId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ProviderVerifiedAtUtc",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "VerifiedAmount",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "VerifiedCurrencyId",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "ActivePaymentTransactionId",
                schema: "deliveries",
                table: "Deliveries");
        }
    }
}
