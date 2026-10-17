using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedRecurringPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecurringPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderId = table.Column<int>(type: "int", nullable: true),
                    Identifier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CardId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastPeriod = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    LastAttemptDay = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringPayments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringPayments_BillProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "BillProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringPayments_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayments_CardId",
                table: "RecurringPayments",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayments_IsActive_LastPeriod",
                table: "RecurringPayments",
                columns: new[] { "IsActive", "LastPeriod" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayments_ProviderId",
                table: "RecurringPayments",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayments_UserId",
                table: "RecurringPayments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringPayments");
        }
    }
}
