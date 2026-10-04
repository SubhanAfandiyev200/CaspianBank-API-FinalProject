using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedBenefitItemAndBenefitSectionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenefitSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenefitItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ButtonText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ButtonUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Text1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Text2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Text3 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BenefitSectionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenefitItems_BenefitSections_BenefitSectionId",
                        column: x => x.BenefitSectionId,
                        principalTable: "BenefitSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitItems_BenefitSectionId",
                table: "BenefitItems",
                column: "BenefitSectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenefitItems");

            migrationBuilder.DropTable(
                name: "BenefitSections");
        }
    }
}
