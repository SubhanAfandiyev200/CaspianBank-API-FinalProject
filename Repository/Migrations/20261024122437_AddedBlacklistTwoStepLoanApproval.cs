using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedBlacklistTwoStepLoanApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FirstApprovedAmount",
                table: "Loans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstApprovedAt",
                table: "Loans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstApprovedByUserId",
                table: "Loans",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresSuperAdminApproval",
                table: "Loans",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstApprovedAmount",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "FirstApprovedAt",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "FirstApprovedByUserId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "RequiresSuperAdminApproval",
                table: "Loans");
        }
    }
}
