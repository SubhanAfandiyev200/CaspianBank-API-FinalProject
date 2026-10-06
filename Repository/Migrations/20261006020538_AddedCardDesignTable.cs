using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedCardDesignTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardDesigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Image = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ShowOnHome = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardDesigns", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CardDesigns",
                columns: new[] { "Id", "CreatedAt", "DisplayOrder", "Image", "ShowOnHome", "Title" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Utc), 1, "/images/cards/regular.png", true, "Regular" },
                    { 2, new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Utc), 2, "/images/cards/silver.png", true, "Silver" },
                    { 3, new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Utc), 3, "/images/cards/gold.png", true, "Gold" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardDesigns_ShowOnHome_DisplayOrder",
                table: "CardDesigns",
                columns: new[] { "ShowOnHome", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardDesigns");
        }
    }
}
