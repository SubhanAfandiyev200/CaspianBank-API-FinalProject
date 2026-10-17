using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedTreasury : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TreasuryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreasuryEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryEntries_CreatedAt",
                table: "TreasuryEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryEntries_Kind",
                table: "TreasuryEntries",
                column: "Kind");

            // Xəzinənin keçmişi: başlanğıc kapital (OwnBanks.OwnFunds) və indiyə qədər olan əməliyyatlardan hesablanan sətirlər (vaxt sırası ilə)
            migrationBuilder.Sql(@"
INSERT INTO TreasuryEntries (Kind, Amount, Description, Reference, CreatedAt)
SELECT 0, OwnFunds, 'Starting capital', 'CAPITAL',
       DATEADD(minute, -1, ISNULL((SELECT MIN(d) FROM (SELECT MIN(CreatedAt) AS d FROM Transactions UNION ALL SELECT MIN(PaidAt) FROM LoanPaymentRecords) AS firsts), GETUTCDATE()))
FROM OwnBanks;

INSERT INTO TreasuryEntries (Kind, Amount, Description, Reference, CreatedAt)
SELECT Kind, Amount, Description, Reference, CreatedAt FROM (
    SELECT 1 AS Kind, Amount AS Amount, 'Transfer commission' AS Description, Reference, CreatedAt FROM Transactions WHERE Type = 3
    UNION ALL SELECT 2, Amount, 'Card opening fee', Reference, CreatedAt FROM Transactions WHERE Type = 4
    UNION ALL SELECT 3, -Amount, 'Loan paid out', Reference, CreatedAt FROM Transactions WHERE Type = 5
    UNION ALL SELECT 10, -Amount, 'Cashback paid to a customer', Reference, CreatedAt FROM Transactions WHERE Type = 8
    UNION ALL SELECT 7, Amount, 'Deposit received', Reference, CreatedAt FROM Transactions WHERE Type = 9
    UNION ALL SELECT 8, -Amount, 'Deposit returned to a customer', Reference, CreatedAt FROM Transactions WHERE Type = 10
    UNION ALL SELECT 9, -Amount, 'Deposit interest paid', Reference, CreatedAt FROM Transactions WHERE Type = 11
    UNION ALL SELECT 4, SUM(PrincipalPart), 'Loan principal repaid', TransactionReference, MIN(PaidAt) FROM LoanPaymentRecords GROUP BY TransactionReference HAVING SUM(PrincipalPart) > 0
    UNION ALL SELECT 5, SUM(InterestPart), 'Loan interest received', TransactionReference, MIN(PaidAt) FROM LoanPaymentRecords GROUP BY TransactionReference HAVING SUM(InterestPart) > 0
    UNION ALL SELECT 6, SUM(PenaltyPart), 'Loan late penalty received', TransactionReference, MIN(PaidAt) FROM LoanPaymentRecords GROUP BY TransactionReference HAVING SUM(PenaltyPart) > 0
) AS history
ORDER BY CreatedAt;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreasuryEntries");
        }
    }
}
