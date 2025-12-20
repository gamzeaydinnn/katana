using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderMappingWithExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Customers_CustomerId",
                table: "SalesOrders");

            migrationBuilder.AddColumn<string>(
                name: "ExternalOrderId",
                table: "OrderMappings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "OrderMappings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_IsSyncedToLuca",
                table: "SalesOrders",
                column: "IsSyncedToLuca");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_KatanaOrderId",
                table: "SalesOrders",
                column: "KatanaOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_LucaOrderId",
                table: "SalesOrders",
                column: "LucaOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OrderNo",
                table: "SalesOrders",
                column: "OrderNo");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMappings_ExternalOrderId",
                table: "OrderMappings",
                column: "ExternalOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Customers_CustomerId",
                table: "SalesOrders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Customers_CustomerId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_IsSyncedToLuca",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_KatanaOrderId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_LucaOrderId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_OrderNo",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_OrderMappings_ExternalOrderId",
                table: "OrderMappings");

            migrationBuilder.DropColumn(
                name: "ExternalOrderId",
                table: "OrderMappings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OrderMappings");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Customers_CustomerId",
                table: "SalesOrders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
