using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvatarUploads",
                schema: "stakeholders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpectedContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpectedContentLength = table.Column<long>(type: "bigint", nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    QuarantineObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FinalObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FinalUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ValidatedETag = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_AvatarUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarUploads_Stakeholders_StakeholderId",
                        column: x => x.StakeholderId,
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvatarUploads_StakeholderId",
                schema: "stakeholders",
                table: "AvatarUploads",
                column: "StakeholderId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarUploads_Status_ExpiresAtUtc",
                schema: "stakeholders",
                table: "AvatarUploads",
                columns: new[] { "Status", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvatarUploads",
                schema: "stakeholders");
        }
    }
}
