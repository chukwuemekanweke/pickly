using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryBookingConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryQuoteId",
                schema: "deliveries",
                table: "Deliveries",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleCategoryId",
                schema: "deliveries",
                table: "Deliveries",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Deliveries_Id_DeliveryQuoteId",
                schema: "deliveries",
                table: "Deliveries",
                columns: new[] { "Id", "DeliveryQuoteId" });

            migrationBuilder.CreateTable(
                name: "DeliveryBookingConfirmations",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryQuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KeyHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                name: "IX_DeliveryQuotes_ConsumedByDeliveryId_Id",
                schema: "deliveries",
                table: "DeliveryQuotes",
                columns: new[] { "ConsumedByDeliveryId", "Id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_DeliveryQuotes_Consumption",
                schema: "deliveries",
                table: "DeliveryQuotes",
                sql: "(\"ConsumptionStatus\" = 1 AND \"ConsumedByDeliveryId\" IS NULL AND \"ConsumedAtUtc\" IS NULL) OR (\"ConsumptionStatus\" = 2 AND \"ConsumedByDeliveryId\" IS NOT NULL AND \"ConsumedAtUtc\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_DeliveryQuoteId",
                schema: "deliveries",
                table: "Deliveries",
                column: "DeliveryQuoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_VehicleCategoryId",
                schema: "deliveries",
                table: "Deliveries",
                column: "VehicleCategoryId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_DeliveryQuotes_DeliveryQuoteId",
                schema: "deliveries",
                table: "Deliveries",
                column: "DeliveryQuoteId",
                principalSchema: "deliveries",
                principalTable: "DeliveryQuotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_VehicleCategories_VehicleCategoryId",
                schema: "deliveries",
                table: "Deliveries",
                column: "VehicleCategoryId",
                principalSchema: "deliveries",
                principalTable: "VehicleCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryQuotes_Deliveries_ConsumedByDeliveryId_Id",
                schema: "deliveries",
                table: "DeliveryQuotes",
                columns: new[] { "ConsumedByDeliveryId", "Id" },
                principalSchema: "deliveries",
                principalTable: "Deliveries",
                principalColumns: new[] { "Id", "DeliveryQuoteId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_DeliveryQuotes_DeliveryQuoteId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_VehicleCategories_VehicleCategoryId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryQuotes_Deliveries_ConsumedByDeliveryId_Id",
                schema: "deliveries",
                table: "DeliveryQuotes");

            migrationBuilder.DropTable(
                name: "DeliveryBookingConfirmations",
                schema: "deliveries");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryQuotes_ConsumedByDeliveryId_Id",
                schema: "deliveries",
                table: "DeliveryQuotes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeliveryQuotes_Consumption",
                schema: "deliveries",
                table: "DeliveryQuotes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Deliveries_Id_DeliveryQuoteId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_DeliveryQuoteId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_VehicleCategoryId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DeliveryQuoteId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "VehicleCategoryId",
                schema: "deliveries",
                table: "Deliveries");
        }
    }
}
