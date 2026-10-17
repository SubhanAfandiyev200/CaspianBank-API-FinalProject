using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedLoans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EarlyRepaymentMinPercent",
                table: "OwnBanks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "EarlyRepaymentRateReduction",
                table: "OwnBanks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinAnnualInterestRate",
                table: "OwnBanks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 5m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinEarlyPayment",
                table: "OwnBanks",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<int>(
                name: "OfferValidDays",
                table: "OwnBanks",
                type: "int",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnBankId = table.Column<int>(type: "int", nullable: false),
                    BorrowerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    PurposeNote = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContractNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    MonthlyPayment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRepayment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingPrincipal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncomeAtApply = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InitialAnnualInterestRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AnnualInterestRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FixedPenalty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DailyPenaltyPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TargetCardId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OfferExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisbursedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedEarly = table.Column<bool>(type: "bit", nullable: false),
                    CourtAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_AspNetUsers_BorrowerUserId",
                        column: x => x.BorrowerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loans_Cards_TargetCardId",
                        column: x => x.TargetCardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loans_OwnBanks_OwnBankId",
                        column: x => x.OwnBankId,
                        principalTable: "OwnBanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FullyPaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FixedPenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AccruedPenalty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidPenalty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PenaltyUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanInstallments_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanPaymentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<int>(type: "int", nullable: false),
                    InstallmentId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PenaltyPart = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestPart = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrincipalPart = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestRateAfter = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanPaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanPaymentRecords_LoanInstallments_InstallmentId",
                        column: x => x.InstallmentId,
                        principalTable: "LoanInstallments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPaymentRecords_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanInstallments_LoanId_Number",
                table: "LoanInstallments",
                columns: new[] { "LoanId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentRecords_InstallmentId",
                table: "LoanPaymentRecords",
                column: "InstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentRecords_LoanId",
                table: "LoanPaymentRecords",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentRecords_TransactionReference",
                table: "LoanPaymentRecords",
                column: "TransactionReference");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_BorrowerUserId_Status",
                table: "Loans",
                columns: new[] { "BorrowerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ContractNumber",
                table: "Loans",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loans_OwnBankId",
                table: "Loans",
                column: "OwnBankId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_TargetCardId",
                table: "Loans",
                column: "TargetCardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanPaymentRecords");

            migrationBuilder.DropTable(
                name: "LoanInstallments");

            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropColumn(
                name: "EarlyRepaymentMinPercent",
                table: "OwnBanks");

            migrationBuilder.DropColumn(
                name: "EarlyRepaymentRateReduction",
                table: "OwnBanks");

            migrationBuilder.DropColumn(
                name: "MinAnnualInterestRate",
                table: "OwnBanks");

            migrationBuilder.DropColumn(
                name: "MinEarlyPayment",
                table: "OwnBanks");

            migrationBuilder.DropColumn(
                name: "OfferValidDays",
                table: "OwnBanks");
        }
    }
}
