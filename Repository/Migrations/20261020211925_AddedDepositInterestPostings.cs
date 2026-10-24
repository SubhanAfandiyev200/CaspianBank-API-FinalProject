using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedDepositInterestPostings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepositInterestPostings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepositId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositInterestPostings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositInterestPostings_Deposits_DepositId",
                        column: x => x.DepositId,
                        principalTable: "Deposits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepositInterestPostings_CreatedAt",
                table: "DepositInterestPostings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DepositInterestPostings_DepositId_Number",
                table: "DepositInterestPostings",
                columns: new[] { "DepositId", "Number" },
                unique: true);

            // Artıq ödənilmiş depozitlər: faizin hamısı ödəniş gününə bir sətir kimi yazılır (bank xərci həmin ayda görünsün)
            migrationBuilder.Sql(@"
INSERT INTO DepositInterestPostings (DepositId, Number, Amount, CreatedAt)
SELECT Id, TermMonths, ExpectedInterest, PaidAt FROM Deposits WHERE Status = 1 AND PaidAt IS NOT NULL AND ExpectedInterest > 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepositInterestPostings");
        }
    }
}
