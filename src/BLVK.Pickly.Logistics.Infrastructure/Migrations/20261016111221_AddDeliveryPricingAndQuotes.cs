using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryPricingAndQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryPricingPolicies",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DistanceRatePerKilometre = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    WeightRatePerKilogram = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveryType = table.Column<int>(type: "integer", nullable: true),
                    OrderType = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_DeliveryPricingPolicies", x => x.Id);
                    table.CheckConstraint("CK_DeliveryPricingPolicies_EffectivePeriod", "\"EffectiveToUtc\" IS NULL OR \"EffectiveToUtc\" > \"EffectiveFromUtc\"");
                    table.CheckConstraint("CK_DeliveryPricingPolicies_NonnegativeRates", "\"BaseFee\" >= 0 AND \"DistanceRatePerKilometre\" >= 0 AND \"WeightRatePerKilogram\" >= 0");
                    table.ForeignKey(
                        name: "FK_DeliveryPricingPolicies_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "payments",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryZones",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Boundary = table.Column<MultiPolygon>(type: "geometry(MultiPolygon,4326)", nullable: false),
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
                    table.PrimaryKey("PK_DeliveryZones", x => x.Id);
                    table.CheckConstraint("CK_DeliveryZones_EffectivePeriod", "\"EffectiveToUtc\" IS NULL OR \"EffectiveToUtc\" > \"EffectiveFromUtc\"");
                });

            migrationBuilder.CreateTable(
                name: "VehicleCategories",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MaximumWeightKg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    RoutingProfile = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_VehicleCategories", x => x.Id);
                    table.CheckConstraint("CK_VehicleCategories_MaximumWeightKg", "\"MaximumWeightKg\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "DeliveryZonePricingRules",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryPricingPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    Charge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_DeliveryZonePricingRules", x => x.Id);
                    table.CheckConstraint("CK_DeliveryZonePricingRules_Charge", "\"Charge\" >= 0");
                    table.ForeignKey(
                        name: "FK_DeliveryZonePricingRules_DeliveryPricingPolicies_DeliveryPr~",
                        column: x => x.DeliveryPricingPolicyId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryPricingPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryZonePricingRules_DeliveryZones_DestinationZoneId",
                        column: x => x.DestinationZoneId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryZonePricingRules_DeliveryZones_OriginZoneId",
                        column: x => x.OriginZoneId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryQuotes",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickupLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DropOffLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickupAddressLine = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PickupAdditionalInformation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PickupCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupCountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    PickupLatitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    PickupLongitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    DropOffAddressLine = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DropOffAdditionalInformation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DropOffCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DropOffState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DropOffCountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    DropOffLatitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    DropOffLongitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    DeliveryType = table.Column<int>(type: "integer", nullable: false),
                    OrderType = table.Column<int>(type: "integer", nullable: false),
                    ScheduledPickupAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VehicleCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleCategoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VehicleCategoryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalWeightKg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    OriginZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginZoneCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DestinationZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationZoneCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PricingPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingPolicyVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DistanceCharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WeightCharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VehicleCharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ZoneCharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumptionStatus = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ConsumedByDeliveryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsumedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_DeliveryQuotes", x => x.Id);
                    table.CheckConstraint("CK_DeliveryQuotes_Expiry", "\"ExpiresAtUtc\" > \"IssuedAtUtc\"");
                    table.CheckConstraint("CK_DeliveryQuotes_Total", "\"TotalAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_DeliveryQuotes_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "payments",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryQuotes_DeliveryPricingPolicies_PricingPolicyId",
                        column: x => x.PricingPolicyId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryPricingPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryQuotes_DeliveryZones_DestinationZoneId",
                        column: x => x.DestinationZoneId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryQuotes_DeliveryZones_OriginZoneId",
                        column: x => x.OriginZoneId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryQuotes_Stakeholders_SenderId",
                        column: x => x.SenderId,
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryQuotes_VehicleCategories_VehicleCategoryId",
                        column: x => x.VehicleCategoryId,
                        principalSchema: "deliveries",
                        principalTable: "VehicleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryVehiclePricingRules",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryPricingPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Charge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_DeliveryVehiclePricingRules", x => x.Id);
                    table.CheckConstraint("CK_DeliveryVehiclePricingRules_Charge", "\"Charge\" >= 0");
                    table.ForeignKey(
                        name: "FK_DeliveryVehiclePricingRules_DeliveryPricingPolicies_Deliver~",
                        column: x => x.DeliveryPricingPolicyId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryPricingPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryVehiclePricingRules_VehicleCategories_VehicleCatego~",
                        column: x => x.VehicleCategoryId,
                        principalSchema: "deliveries",
                        principalTable: "VehicleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryQuoteItems",
                schema: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryQuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
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
                    table.PrimaryKey("PK_DeliveryQuoteItems", x => x.Id);
                    table.CheckConstraint("CK_DeliveryQuoteItems_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_DeliveryQuoteItems_WeightKg", "\"WeightKg\" > 0");
                    table.ForeignKey(
                        name: "FK_DeliveryQuoteItems_DeliveryQuotes_DeliveryQuoteId",
                        column: x => x.DeliveryQuoteId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPricingPolicies_CurrencyId",
                schema: "deliveries",
                table: "DeliveryPricingPolicies",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPricingPolicies_Version",
                schema: "deliveries",
                table: "DeliveryPricingPolicies",
                column: "Version",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQuoteItems_DeliveryQuoteId",
                schema: "deliveries",
                table: "DeliveryQuoteItems",
                column: "DeliveryQuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQuotes_ConsumedByDeliveryId",
                schema: "deliveries",
                table: "DeliveryQuotes",
                column: "ConsumedByDeliveryId",
                unique: true,
                filter: "\"ConsumedByDeliveryId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQuotes_CurrencyId",
                schema: "deliveries",
                table: "DeliveryQuotes",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQuotes_DestinationZoneId",
                schema: "deliveries",
                table: "DeliveryQuotes",
                column: "DestinationZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQuotes_OriginZoneId",
                schema: "deliveries",
                table: "DeliveryQuotes",
                column: "OriginZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQuotes_PricingPolicyId",
                schema: "deliveries",
                table: "DeliveryQuotes",
                column: "PricingPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQuotes_SenderId_CreatedAtUtc",
                schema: "deliveries",
                table: "DeliveryQuotes",
                columns: new[] { "SenderId", "CreatedAtUtc" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQuotes_VehicleCategoryId",
                schema: "deliveries",
                table: "DeliveryQuotes",
                column: "VehicleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryVehiclePricingRules_DeliveryPricingPolicyId_Vehicle~",
                schema: "deliveries",
                table: "DeliveryVehiclePricingRules",
                columns: new[] { "DeliveryPricingPolicyId", "VehicleCategoryId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryVehiclePricingRules_VehicleCategoryId",
                schema: "deliveries",
                table: "DeliveryVehiclePricingRules",
                column: "VehicleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZonePricingRules_DeliveryPricingPolicyId_OriginZone~",
                schema: "deliveries",
                table: "DeliveryZonePricingRules",
                columns: new[] { "DeliveryPricingPolicyId", "OriginZoneId", "DestinationZoneId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZonePricingRules_DestinationZoneId",
                schema: "deliveries",
                table: "DeliveryZonePricingRules",
                column: "DestinationZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZonePricingRules_OriginZoneId",
                schema: "deliveries",
                table: "DeliveryZonePricingRules",
                column: "OriginZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZones_Boundary",
                schema: "deliveries",
                table: "DeliveryZones",
                column: "Boundary")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZones_CountryCode_Code",
                schema: "deliveries",
                table: "DeliveryZones",
                columns: new[] { "CountryCode", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZones_IsActive_EffectiveFromUtc_EffectiveToUtc",
                schema: "deliveries",
                table: "DeliveryZones",
                columns: new[] { "IsActive", "EffectiveFromUtc", "EffectiveToUtc" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCategories_Code",
                schema: "deliveries",
                table: "VehicleCategories",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCategories_IsActive_MaximumWeightKg",
                schema: "deliveries",
                table: "VehicleCategories",
                columns: new[] { "IsActive", "MaximumWeightKg" },
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryQuoteItems",
                schema: "deliveries");

            migrationBuilder.DropTable(
                name: "DeliveryVehiclePricingRules",
                schema: "deliveries");

            migrationBuilder.DropTable(
                name: "DeliveryZonePricingRules",
                schema: "deliveries");

            migrationBuilder.DropTable(
                name: "DeliveryQuotes",
                schema: "deliveries");

            migrationBuilder.DropTable(
                name: "DeliveryPricingPolicies",
                schema: "deliveries");

            migrationBuilder.DropTable(
                name: "DeliveryZones",
                schema: "deliveries");

            migrationBuilder.DropTable(
                name: "VehicleCategories",
                schema: "deliveries");
        }
    }
}
