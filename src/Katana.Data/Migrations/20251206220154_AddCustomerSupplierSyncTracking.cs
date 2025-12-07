using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSupplierSyncTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "Suppliers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncHash",
                table: "Suppliers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncStatus",
                table: "Suppliers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastSyncHash",
                table: "Customers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncStatus",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING");

            migrationBuilder.CreateTable(
                name: "CustomerKozaCariMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KatanaCustomerId = table.Column<int>(type: "int", nullable: false),
                    KozaCariKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KozaFinansalNesneId = table.Column<long>(type: "bigint", nullable: true),
                    KatanaCustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KozaCariTanim = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KatanaCustomerTaxNo = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    SyncStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    LastSyncHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastSyncError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerKozaCariMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_LastSyncHash",
                table: "Customers",
                column: "LastSyncHash");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_SyncedAt",
                table: "Customers",
                column: "SyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_SyncStatus",
                table: "Customers",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerKozaCariMappings_KatanaCustomerId",
                table: "CustomerKozaCariMappings",
                column: "KatanaCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerKozaCariMappings_KatanaCustomerTaxNo",
                table: "CustomerKozaCariMappings",
                column: "KatanaCustomerTaxNo");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerKozaCariMappings_KozaCariKodu",
                table: "CustomerKozaCariMappings",
                column: "KozaCariKodu");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerKozaCariMappings_KozaFinansalNesneId",
                table: "CustomerKozaCariMappings",
                column: "KozaFinansalNesneId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerKozaCariMappings_LastSyncAt",
                table: "CustomerKozaCariMappings",
                column: "LastSyncAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerKozaCariMappings_SyncStatus",
                table: "CustomerKozaCariMappings",
                column: "SyncStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerKozaCariMappings");

            migrationBuilder.DropIndex(
                name: "IX_Customers_LastSyncHash",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_SyncedAt",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_SyncStatus",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "LastSyncHash",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "LastSyncHash",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "Customers");
        }
    }
}
