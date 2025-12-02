using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationKozaDepotMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocationKozaDepotMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KatanaLocationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KozaDepoKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KozaDepoId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KatanaLocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KozaDepoTanim = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationKozaDepotMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LucaProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LucaCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LucaName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    LucaCategory = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LucaProducts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationKozaDepotMappings_KatanaLocationId",
                table: "LocationKozaDepotMappings",
                column: "KatanaLocationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationKozaDepotMappings_KozaDepoId",
                table: "LocationKozaDepotMappings",
                column: "KozaDepoId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationKozaDepotMappings_KozaDepoKodu",
                table: "LocationKozaDepotMappings",
                column: "KozaDepoKodu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationKozaDepotMappings");

            migrationBuilder.DropTable(
                name: "LucaProducts");
        }
    }
}
