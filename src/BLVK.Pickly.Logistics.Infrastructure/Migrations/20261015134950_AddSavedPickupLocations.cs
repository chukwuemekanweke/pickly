using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedPickupLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_Tenants_TenantId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_SenderId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_TenantId_SenderId_CreatedAtUtc",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.CreateTable(
                name: "SavedPickupLocations",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressLine = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AdditionalInformation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_SavedPickupLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedPickupLocations_Stakeholders_SenderId",
                        column: x => x.SenderId,
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_SenderId_CreatedAtUtc",
                schema: "deliveries",
                table: "Deliveries",
                columns: new[] { "SenderId", "CreatedAtUtc" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPickupLocations_SenderId",
                schema: "deliveries",
                table: "SavedPickupLocations",
                column: "SenderId",
                unique: true,
                filter: "\"IsDefault\" = TRUE AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPickupLocations_SenderId_CreatedAtUtc_Id",
                schema: "deliveries",
                table: "SavedPickupLocations",
                columns: new[] { "SenderId", "CreatedAtUtc", "Id" },
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedPickupLocations",
                schema: "deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_SenderId_CreatedAtUtc",
                schema: "deliveries",
                table: "Deliveries");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "deliveries",
                table: "Deliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE deliveries."Deliveries" AS delivery
                SET "TenantId" = stakeholder."TenantId"
                FROM stakeholders."Stakeholders" AS stakeholder
                WHERE delivery."SenderId" = stakeholder."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                schema: "deliveries",
                table: "Deliveries",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_SenderId",
                schema: "deliveries",
                table: "Deliveries",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_TenantId_SenderId_CreatedAtUtc",
                schema: "deliveries",
                table: "Deliveries",
                columns: new[] { "TenantId", "SenderId", "CreatedAtUtc" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_Tenants_TenantId",
                schema: "deliveries",
                table: "Deliveries",
                column: "TenantId",
                principalSchema: "stakeholders",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
