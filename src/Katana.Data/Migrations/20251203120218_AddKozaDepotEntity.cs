using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKozaDepotEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PurchaseOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentSeries",
                table: "PurchaseOrders",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentTypeDetailId",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSyncedToLuca",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "PurchaseOrders",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LucaDocumentNo",
                table: "PurchaseOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LucaPurchaseOrderId",
                table: "PurchaseOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectCode",
                table: "PurchaseOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceCode",
                table: "PurchaseOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShippingAddressId",
                table: "PurchaseOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncRetryCount",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "VatIncluded",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "PurchaseOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "LucaDetailId",
                table: "PurchaseOrderItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LucaStockCode",
                table: "PurchaseOrderItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitCode",
                table: "PurchaseOrderItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "PurchaseOrderItems",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseCode",
                table: "PurchaseOrderItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "KozaDepots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepoId = table.Column<long>(type: "bigint", nullable: true),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tanim = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    KategoriKod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Ulke = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Il = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Ilce = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AdresSerbest = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KozaDepots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedAt",
                table: "PurchaseOrders",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_KozaDepots_DepoId",
                table: "KozaDepots",
                column: "DepoId");

            migrationBuilder.CreateIndex(
                name: "IX_KozaDepots_Kod",
                table: "KozaDepots",
                column: "Kod");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KozaDepots");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CreatedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DocumentSeries",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DocumentTypeDetailId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsSyncedToLuca",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "LucaDocumentNo",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "LucaPurchaseOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ProjectCode",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SyncRetryCount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "VatIncluded",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "LucaDetailId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "LucaStockCode",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "UnitCode",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "WarehouseCode",
                table: "PurchaseOrderItems");
        }
    }
}
