using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepositPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    AnnualRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Deposits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: true),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    AnnualRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Principal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedInterest = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaturesAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceCardId = table.Column<int>(type: "int", nullable: false),
                    PayoutCardId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidToCardId = table.Column<int>(type: "int", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deposits_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deposits_Cards_PayoutCardId",
                        column: x => x.PayoutCardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deposits_Cards_SourceCardId",
                        column: x => x.SourceCardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deposits_DepositPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "DepositPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepositPlans_TermMonths",
                table: "DepositPlans",
                column: "TermMonths",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_PayoutCardId",
                table: "Deposits",
                column: "PayoutCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_PlanId",
                table: "Deposits",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_Reference",
                table: "Deposits",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_SourceCardId",
                table: "Deposits",
                column: "SourceCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_Status_MaturesAt",
                table: "Deposits",
                columns: new[] { "Status", "MaturesAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_UserId_Status",
                table: "Deposits",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.Sql("INSERT INTO DepositPlans (TermMonths, AnnualRate, MinAmount, MaxAmount, IsActive, CreatedAt) VALUES (3, 3, 100, 100000, 1, GETUTCDATE()), (6, 4, 100, 100000, 1, GETUTCDATE()), (12, 5, 100, 100000, 1, GETUTCDATE()), (24, 6, 100, 100000, 1, GETUTCDATE())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Deposits");

            migrationBuilder.DropTable(
                name: "DepositPlans");
        }
    }
}
