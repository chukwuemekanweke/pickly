using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BLVK.Pickly.Logistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTransactionReportingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "payments",
                table: "WalletTransactions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransactionCategory",
                schema: "payments",
                table: "WalletTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "TransactionTitle",
                schema: "payments",
                table: "WalletTransactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Wallet funding");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_CreatedAtUtc_Id",
                schema: "payments",
                table: "WalletTransactions",
                columns: new[] { "CreatedAtUtc", "Id" },
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_CreatedAtUtc_Id",
                schema: "payments",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "payments",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionCategory",
                schema: "payments",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionTitle",
                schema: "payments",
                table: "WalletTransactions");
        }
    }
}
