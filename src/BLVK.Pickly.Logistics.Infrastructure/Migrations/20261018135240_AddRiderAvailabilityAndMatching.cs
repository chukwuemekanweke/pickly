using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRiderAvailabilityAndMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "riders");

            migrationBuilder.CreateTable(
                name: "DeliveryMatchingSessions",
                schema: "riders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SearchRadiusMeters = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_DeliveryMatchingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryMatchingSessions_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "deliveries",
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderProfiles",
                schema: "riders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailabilityStatus = table.Column<int>(type: "integer", nullable: false),
                    VerificationStatus = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_RiderProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderProfiles_Stakeholders_StakeholderId",
                        column: x => x.StakeholderId,
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "stakeholders",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderProfiles_VehicleCategories_VehicleCategoryId",
                        column: x => x.VehicleCategoryId,
                        principalSchema: "deliveries",
                        principalTable: "VehicleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderCurrentLocations",
                schema: "riders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Position = table.Column<Point>(type: "geography(Point,4326)", nullable: false),
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
                    table.PrimaryKey("PK_RiderCurrentLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderCurrentLocations_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "riders",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiderDeliveryOffers",
                schema: "riders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchingSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OfferedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_RiderDeliveryOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderDeliveryOffers_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "deliveries",
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderDeliveryOffers_DeliveryMatchingSessions_MatchingSessio~",
                        column: x => x.MatchingSessionId,
                        principalSchema: "riders",
                        principalTable: "DeliveryMatchingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderDeliveryOffers_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "riders",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderServiceZones",
                schema: "riders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryZoneId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_RiderServiceZones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderServiceZones_DeliveryZones_DeliveryZoneId",
                        column: x => x.DeliveryZoneId,
                        principalSchema: "deliveries",
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderServiceZones_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "riders",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_RiderId",
                schema: "deliveries",
                table: "Deliveries",
                column: "RiderId",
                unique: true,
                filter: "\"RiderId\" IS NOT NULL AND \"Status\" IN (4, 5, 6) AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMatchingSessions_DeliveryId",
                schema: "riders",
                table: "DeliveryMatchingSessions",
                column: "DeliveryId",
                unique: true,
                filter: "\"Status\" = 1 AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMatchingSessions_Status_NextAttemptAtUtc",
                schema: "riders",
                table: "DeliveryMatchingSessions",
                columns: new[] { "Status", "NextAttemptAtUtc" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCurrentLocations_CapturedAtUtc",
                schema: "riders",
                table: "RiderCurrentLocations",
                column: "CapturedAtUtc",
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCurrentLocations_Position",
                schema: "riders",
                table: "RiderCurrentLocations",
                column: "Position")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCurrentLocations_RiderProfileId",
                schema: "riders",
                table: "RiderCurrentLocations",
                column: "RiderProfileId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RiderDeliveryOffers_DeliveryId",
                schema: "riders",
                table: "RiderDeliveryOffers",
                column: "DeliveryId",
                unique: true,
                filter: "\"Status\" = 1 AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RiderDeliveryOffers_MatchingSessionId",
                schema: "riders",
                table: "RiderDeliveryOffers",
                column: "MatchingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderDeliveryOffers_RiderProfileId",
                schema: "riders",
                table: "RiderDeliveryOffers",
                column: "RiderProfileId",
                unique: true,
                filter: "\"Status\" = 1 AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RiderDeliveryOffers_Status_ExpiresAtUtc",
                schema: "riders",
                table: "RiderDeliveryOffers",
                columns: new[] { "Status", "ExpiresAtUtc" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_StakeholderId",
                schema: "riders",
                table: "RiderProfiles",
                column: "StakeholderId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_TenantId_AvailabilityStatus_VehicleCategoryId",
                schema: "riders",
                table: "RiderProfiles",
                columns: new[] { "TenantId", "AvailabilityStatus", "VehicleCategoryId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_VehicleCategoryId",
                schema: "riders",
                table: "RiderProfiles",
                column: "VehicleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderServiceZones_DeliveryZoneId",
                schema: "riders",
                table: "RiderServiceZones",
                column: "DeliveryZoneId",
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RiderServiceZones_RiderProfileId_DeliveryZoneId",
                schema: "riders",
                table: "RiderServiceZones",
                columns: new[] { "RiderProfileId", "DeliveryZoneId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiderCurrentLocations",
                schema: "riders");

            migrationBuilder.DropTable(
                name: "RiderDeliveryOffers",
                schema: "riders");

            migrationBuilder.DropTable(
                name: "RiderServiceZones",
                schema: "riders");

            migrationBuilder.DropTable(
                name: "DeliveryMatchingSessions",
                schema: "riders");

            migrationBuilder.DropTable(
                name: "RiderProfiles",
                schema: "riders");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_RiderId",
                schema: "deliveries",
                table: "Deliveries");
        }
    }
}
