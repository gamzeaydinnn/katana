using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxRateAndUoMMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BelgeNo",
                table: "OrderMappings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BelgeSeri",
                table: "OrderMappings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BelgeTakipNo",
                table: "OrderMappings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaxRateMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KatanaTaxRateId = table.Column<long>(type: "bigint", nullable: false),
                    KozaKdvOran = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSyncHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SyncStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRateMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UoMMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KatanaUoMString = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KozaOlcumBirimiId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSyncHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SyncStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UoMMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VariantMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KatanaVariantId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRateMappings_KatanaTaxRateId",
                table: "TaxRateMappings",
                column: "KatanaTaxRateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRateMappings_LastSyncAt",
                table: "TaxRateMappings",
                column: "LastSyncAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRateMappings_SyncStatus",
                table: "TaxRateMappings",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_UoMMappings_KatanaUoMString",
                table: "UoMMappings",
                column: "KatanaUoMString",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UoMMappings_LastSyncAt",
                table: "UoMMappings",
                column: "LastSyncAt");

            migrationBuilder.CreateIndex(
                name: "IX_UoMMappings_SyncStatus",
                table: "UoMMappings",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_VariantMappings_KatanaVariantId",
                table: "VariantMappings",
                column: "KatanaVariantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxRateMappings");

            migrationBuilder.DropTable(
                name: "UoMMappings");

            migrationBuilder.DropTable(
                name: "VariantMappings");

            migrationBuilder.DropColumn(
                name: "BelgeNo",
                table: "OrderMappings");

            migrationBuilder.DropColumn(
                name: "BelgeSeri",
                table: "OrderMappings");

            migrationBuilder.DropColumn(
                name: "BelgeTakipNo",
                table: "OrderMappings");
        }
    }
}
