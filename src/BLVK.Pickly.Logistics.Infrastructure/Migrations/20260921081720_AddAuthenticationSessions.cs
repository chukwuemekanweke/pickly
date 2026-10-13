using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AuthenticationSessionId",
                schema: "authentication",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sessions",
                schema: "authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastActiveAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DevicePlatform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BrowserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstIpAddressId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastIpAddressId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_IpAddresses_FirstIpAddressId",
                        column: x => x.FirstIpAddressId,
                        principalSchema: "authentication",
                        principalTable: "IpAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_IpAddresses_LastIpAddressId",
                        column: x => x.LastIpAddressId,
                        principalSchema: "authentication",
                        principalTable: "IpAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Stakeholders_StakeholderId",
                        column: x => x.StakeholderId,
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AuthenticationSessionId",
                schema: "authentication",
                table: "RefreshTokens",
                column: "AuthenticationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ExpiresAtUtc",
                schema: "authentication",
                table: "Sessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_FirstIpAddressId",
                schema: "authentication",
                table: "Sessions",
                column: "FirstIpAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LastActiveAtUtc",
                schema: "authentication",
                table: "Sessions",
                column: "LastActiveAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LastIpAddressId",
                schema: "authentication",
                table: "Sessions",
                column: "LastIpAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StakeholderId",
                schema: "authentication",
                table: "Sessions",
                column: "StakeholderId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Sessions_AuthenticationSessionId",
                schema: "authentication",
                table: "RefreshTokens",
                column: "AuthenticationSessionId",
                principalSchema: "authentication",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Sessions_AuthenticationSessionId",
                schema: "authentication",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Sessions",
                schema: "authentication");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_AuthenticationSessionId",
                schema: "authentication",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "AuthenticationSessionId",
                schema: "authentication",
                table: "RefreshTokens");
        }
    }
}
