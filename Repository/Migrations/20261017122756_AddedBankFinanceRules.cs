using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedBankFinanceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OperatingCostMonthly",
                table: "OwnBanks",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OwnFunds",
                table: "OwnBanks",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitTaxPercent",
                table: "OwnBanks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReservePercent",
                table: "OwnBanks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Mövcud bank sətri üçün başlanğıc maliyyə qaydaları (yeni sütunların defoltu 0 olur)
            migrationBuilder.Sql("UPDATE OwnBanks SET OwnFunds = 2400000, OperatingCostMonthly = 150, ProfitTaxPercent = 20, ReservePercent = 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperatingCostMonthly",
                table: "OwnBanks");

            migrationBuilder.DropColumn(
                name: "OwnFunds",
                table: "OwnBanks");

            migrationBuilder.DropColumn(
                name: "ProfitTaxPercent",
                table: "OwnBanks");

            migrationBuilder.DropColumn(
                name: "ReservePercent",
                table: "OwnBanks");
        }
    }
}
