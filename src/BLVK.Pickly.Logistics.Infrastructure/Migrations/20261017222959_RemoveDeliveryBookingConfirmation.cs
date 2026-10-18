using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeliveryBookingConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryBookingConfirmations",
                schema: "deliveries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryBookingConfirmations",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryQuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    KeyHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryBookingConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryBookingConfirmations_Deliveries_DeliveryId_Delivery~",
                        columns: x => new { x.DeliveryId, x.DeliveryQuoteId },
                        principalSchema: "deliveries",
                        principalTable: "Deliveries",
                        principalColumns: new[] { "Id", "DeliveryQuoteId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryBookingConfirmations_DeliveryQuotes_DeliveryQuoteId",
                        column: x => x.DeliveryQuoteId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryBookingConfirmations_Stakeholders_SenderId",
                        column: x => x.SenderId,
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBookingConfirmations_DeliveryId",
                schema: "deliveries",
                table: "DeliveryBookingConfirmations",
                column: "DeliveryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBookingConfirmations_DeliveryId_DeliveryQuoteId",
                schema: "deliveries",
                table: "DeliveryBookingConfirmations",
                columns: new[] { "DeliveryId", "DeliveryQuoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBookingConfirmations_DeliveryQuoteId",
                schema: "deliveries",
                table: "DeliveryBookingConfirmations",
                column: "DeliveryQuoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBookingConfirmations_ExpiresAtUtc",
                schema: "deliveries",
                table: "DeliveryBookingConfirmations",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBookingConfirmations_SenderId_Operation_KeyHash",
                schema: "deliveries",
                table: "DeliveryBookingConfirmations",
                columns: new[] { "SenderId", "Operation", "KeyHash" },
                unique: true);
        }
    }
}
