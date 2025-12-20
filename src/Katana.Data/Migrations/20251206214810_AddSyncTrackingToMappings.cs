using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncTrackingToMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "SupplierKozaCariMappings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "SupplierKozaCariMappings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncHash",
                table: "SupplierKozaCariMappings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncStatus",
                table: "SupplierKozaCariMappings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING");

            migrationBuilder.AlterColumn<string>(
                name: "SyncStatus",
                table: "ProductLucaMappings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LucaStockCode",
                table: "ProductLucaMappings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "KatanaSku",
                table: "ProductLucaMappings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "KatanaProductId",
                table: "ProductLucaMappings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "LastSyncHash",
                table: "ProductLucaMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ProductLucaMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "OrderMappings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "OrderMappings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncHash",
                table: "OrderMappings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncStatus",
                table: "OrderMappings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SYNCED");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "MappingTables",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "MappingTables",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncHash",
                table: "MappingTables",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncStatus",
                table: "MappingTables",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LocationKozaDepotMappings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "LocationKozaDepotMappings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "LocationKozaDepotMappings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncHash",
                table: "LocationKozaDepotMappings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncStatus",
                table: "LocationKozaDepotMappings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierKozaCariMappings_LastSyncAt",
                table: "SupplierKozaCariMappings",
                column: "LastSyncAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierKozaCariMappings_SyncStatus",
                table: "SupplierKozaCariMappings",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLucaMappings_KatanaProductId_IsActive",
                table: "ProductLucaMappings",
                columns: new[] { "KatanaProductId", "IsActive" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductLucaMappings_LucaStockCode",
                table: "ProductLucaMappings",
                column: "LucaStockCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLucaMappings_SyncedAt",
                table: "ProductLucaMappings",
                column: "SyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLucaMappings_SyncStatus",
                table: "ProductLucaMappings",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMappings_LastSyncAt",
                table: "OrderMappings",
                column: "LastSyncAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMappings_SyncStatus",
                table: "OrderMappings",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MappingTables_LastSyncAt",
                table: "MappingTables",
                column: "LastSyncAt");

            migrationBuilder.CreateIndex(
                name: "IX_MappingTables_SyncStatus",
                table: "MappingTables",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_LocationKozaDepotMappings_LastSyncAt",
                table: "LocationKozaDepotMappings",
                column: "LastSyncAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationKozaDepotMappings_SyncStatus",
                table: "LocationKozaDepotMappings",
                column: "SyncStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierKozaCariMappings_LastSyncAt",
                table: "SupplierKozaCariMappings");

            migrationBuilder.DropIndex(
                name: "IX_SupplierKozaCariMappings_SyncStatus",
                table: "SupplierKozaCariMappings");

            migrationBuilder.DropIndex(
                name: "IX_ProductLucaMappings_KatanaProductId_IsActive",
                table: "ProductLucaMappings");

            migrationBuilder.DropIndex(
                name: "IX_ProductLucaMappings_LucaStockCode",
                table: "ProductLucaMappings");

            migrationBuilder.DropIndex(
                name: "IX_ProductLucaMappings_SyncedAt",
                table: "ProductLucaMappings");

            migrationBuilder.DropIndex(
                name: "IX_ProductLucaMappings_SyncStatus",
                table: "ProductLucaMappings");

            migrationBuilder.DropIndex(
                name: "IX_OrderMappings_LastSyncAt",
                table: "OrderMappings");

            migrationBuilder.DropIndex(
                name: "IX_OrderMappings_SyncStatus",
                table: "OrderMappings");

            migrationBuilder.DropIndex(
                name: "IX_MappingTables_LastSyncAt",
                table: "MappingTables");

            migrationBuilder.DropIndex(
                name: "IX_MappingTables_SyncStatus",
                table: "MappingTables");

            migrationBuilder.DropIndex(
                name: "IX_LocationKozaDepotMappings_LastSyncAt",
                table: "LocationKozaDepotMappings");

            migrationBuilder.DropIndex(
                name: "IX_LocationKozaDepotMappings_SyncStatus",
                table: "LocationKozaDepotMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "SupplierKozaCariMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "SupplierKozaCariMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncHash",
                table: "SupplierKozaCariMappings");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "SupplierKozaCariMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncHash",
                table: "ProductLucaMappings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ProductLucaMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "OrderMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "OrderMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncHash",
                table: "OrderMappings");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "OrderMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "MappingTables");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "MappingTables");

            migrationBuilder.DropColumn(
                name: "LastSyncHash",
                table: "MappingTables");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "MappingTables");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LocationKozaDepotMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "LocationKozaDepotMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "LocationKozaDepotMappings");

            migrationBuilder.DropColumn(
                name: "LastSyncHash",
                table: "LocationKozaDepotMappings");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "LocationKozaDepotMappings");

            migrationBuilder.AlterColumn<string>(
                name: "SyncStatus",
                table: "ProductLucaMappings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<string>(
                name: "LucaStockCode",
                table: "ProductLucaMappings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "KatanaSku",
                table: "ProductLucaMappings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "KatanaProductId",
                table: "ProductLucaMappings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
