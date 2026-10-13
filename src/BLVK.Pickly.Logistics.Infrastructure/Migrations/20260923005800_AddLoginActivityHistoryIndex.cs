using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginActivityHistoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_TenantId_StakeholderId_OccurredAtUtc_Id",
                schema: "authentication",
                table: "LoginActivities",
                columns: new[] { "TenantId", "StakeholderId", "OccurredAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoginActivities_TenantId_StakeholderId_OccurredAtUtc_Id",
                schema: "authentication",
                table: "LoginActivities");
        }
    }
}
