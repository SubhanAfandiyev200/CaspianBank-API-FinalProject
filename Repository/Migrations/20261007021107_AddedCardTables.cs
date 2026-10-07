using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedCardTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    CardDesignId = table.Column<int>(type: "int", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cards_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cards_CardDesigns_CardDesignId",
                        column: x => x.CardDesignId,
                        principalTable: "CardDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CardTierConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    IssueFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashbackPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransferLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CardDesignId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTierConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardTierConfigs_CardDesigns_CardDesignId",
                        column: x => x.CardDesignId,
                        principalTable: "CardDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CardDesigns",
                columns: new[] { "Id", "CreatedAt", "DisplayOrder", "Image", "ShowOnHome", "Title" },
                values: new object[] { 4, new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Utc), 4, "/images/cards/cashback.png", false, "Cashback" });

            migrationBuilder.InsertData(
                table: "CardTierConfigs",
                columns: new[] { "Id", "CardDesignId", "CashbackPercent", "CommissionPercent", "CreatedAt", "IssueFee", "Tier", "TransferLimit" },
                values: new object[,]
                {
                    { 2, 1, 0.5m, 1m, new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Utc), 10m, 1, 500m },
                    { 3, 2, 1m, 0.6m, new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Utc), 15m, 2, 2000m },
                    { 4, 3, 1.5m, 0.3m, new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Utc), 40m, 3, 10000m },
                    { 1, 4, 0m, 0m, new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Utc), 0m, 0, 0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_CardDesignId",
                table: "Cards",
                column: "CardDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_CardNumber",
                table: "Cards",
                column: "CardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_UserId",
                table: "Cards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_Cards_UserId_Cashback",
                table: "Cards",
                column: "UserId",
                unique: true,
                filter: "[Tier] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CardTierConfigs_CardDesignId",
                table: "CardTierConfigs",
                column: "CardDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTierConfigs_Tier",
                table: "CardTierConfigs",
                column: "Tier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "CardTierConfigs");

            migrationBuilder.DeleteData(
                table: "CardDesigns",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
