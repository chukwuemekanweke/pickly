using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedLocationRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine",
                schema: "deliveries",
                table: "SavedPickupLocations");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "deliveries",
                table: "SavedPickupLocations");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                schema: "deliveries",
                table: "SavedPickupLocations");

            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "deliveries",
                table: "SavedPickupLocations");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "deliveries",
                table: "SavedPickupLocations");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "deliveries",
                table: "SavedPickupLocations");

            migrationBuilder.EnsureSchema(
                name: "locations");

            migrationBuilder.RenameColumn(
                name: "AdditionalInformation",
                schema: "deliveries",
                table: "SavedPickupLocations",
                newName: "AdditionalDirections");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                schema: "deliveries",
                table: "SavedPickupLocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "LocationResolutionLeases",
                schema: "locations",
                columns: table => new
                {
                    ResolutionKey = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationResolutionLeases", x => x.ResolutionKey);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                schema: "locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayAddress = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    NormalizedAddress = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    AddressLine = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Coordinates = table.Column<Point>(type: "geography(Point,4326)", nullable: false),
                    LocationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResolutionSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResolutionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationProviderReferences",
                schema: "locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FeatureId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FeatureType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StorageClassification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_LocationProviderReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationProviderReferences_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "locations",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocationResolutionKeys",
                schema: "locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_LocationResolutionKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationResolutionKeys_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "locations",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedPickupLocations_LocationId",
                schema: "deliveries",
                table: "SavedPickupLocations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationProviderReferences_LocationId",
                schema: "locations",
                table: "LocationProviderReferences",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationProviderReferences_Provider_FeatureId",
                schema: "locations",
                table: "LocationProviderReferences",
                columns: new[] { "Provider", "FeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationResolutionKeys_Key",
                schema: "locations",
                table: "LocationResolutionKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationResolutionKeys_LocationId",
                schema: "locations",
                table: "LocationResolutionKeys",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Coordinates",
                schema: "locations",
                table: "Locations",
                column: "Coordinates")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_NormalizedAddress",
                schema: "locations",
                table: "Locations",
                column: "NormalizedAddress");

            // Expression index backing registry text search; queries must use to_tsvector('simple', "DisplayAddress").
            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_Locations_DisplayAddress_FullText"
                ON locations."Locations"
                USING GIN (to_tsvector('simple'::regconfig, "DisplayAddress"));
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPickupLocations_Locations_LocationId",
                schema: "deliveries",
                table: "SavedPickupLocations",
                column: "LocationId",
                principalSchema: "locations",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS locations."IX_Locations_DisplayAddress_FullText";""");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedPickupLocations_Locations_LocationId",
                schema: "deliveries",
                table: "SavedPickupLocations");

            migrationBuilder.DropTable(
                name: "LocationProviderReferences",
                schema: "locations");

            migrationBuilder.DropTable(
                name: "LocationResolutionKeys",
                schema: "locations");

            migrationBuilder.DropTable(
                name: "LocationResolutionLeases",
                schema: "locations");

            migrationBuilder.DropTable(
                name: "Locations",
                schema: "locations");

            migrationBuilder.DropIndex(
                name: "IX_SavedPickupLocations_LocationId",
                schema: "deliveries",
                table: "SavedPickupLocations");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "deliveries",
                table: "SavedPickupLocations");

            migrationBuilder.RenameColumn(
                name: "AdditionalDirections",
                schema: "deliveries",
                table: "SavedPickupLocations",
                newName: "AdditionalInformation");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                schema: "deliveries",
                table: "SavedPickupLocations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "deliveries",
                table: "SavedPickupLocations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "deliveries",
                table: "SavedPickupLocations",
                type: "character(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                schema: "deliveries",
                table: "SavedPickupLocations",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                schema: "deliveries",
                table: "SavedPickupLocations",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "State",
                schema: "deliveries",
                table: "SavedPickupLocations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
