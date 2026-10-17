using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedExternalCardsAndTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalBankId = table.Column<int>(type: "int", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(19)", maxLength: 19, nullable: false),
                    HolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalCards_ExternalBanks_ExternalBankId",
                        column: x => x.ExternalBankId,
                        principalTable: "ExternalBanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceCardId = table.Column<int>(type: "int", nullable: false),
                    ExternalCardId = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CardLast4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    HolderMasked = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Commission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceBalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalTransfers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalTransfers_Cards_SourceCardId",
                        column: x => x.SourceCardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalTransfers_ExternalCards_ExternalCardId",
                        column: x => x.ExternalCardId,
                        principalTable: "ExternalCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCards_CardNumber",
                table: "ExternalCards",
                column: "CardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCards_ExternalBankId",
                table: "ExternalCards",
                column: "ExternalBankId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTransfers_ExternalCardId",
                table: "ExternalTransfers",
                column: "ExternalCardId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTransfers_Reference",
                table: "ExternalTransfers",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTransfers_SourceCardId",
                table: "ExternalTransfers",
                column: "SourceCardId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTransfers_UserId_CreatedAt",
                table: "ExternalTransfers",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.Sql("UPDATE OwnBanks SET InterbankFeePercent = 0, InterbankMinFee = NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalTransfers");

            migrationBuilder.DropTable(
                name: "ExternalCards");
        }
    }
}
