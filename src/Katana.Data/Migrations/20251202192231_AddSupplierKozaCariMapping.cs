using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierKozaCariMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierKozaCariMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KatanaSupplierId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KozaCariKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KozaFinansalNesneId = table.Column<long>(type: "bigint", nullable: true),
                    KatanaSupplierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KozaCariTanim = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierKozaCariMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierKozaCariMappings_KatanaSupplierId",
                table: "SupplierKozaCariMappings",
                column: "KatanaSupplierId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierKozaCariMappings_KozaCariKodu",
                table: "SupplierKozaCariMappings",
                column: "KozaCariKodu");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierKozaCariMappings_KozaFinansalNesneId",
                table: "SupplierKozaCariMappings",
                column: "KozaFinansalNesneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierKozaCariMappings");
        }
    }
}
