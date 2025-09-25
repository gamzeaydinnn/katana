using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSyncFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "MappingTables",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 25, 17, 35, 25, 553, DateTimeKind.Utc).AddTicks(781), new DateTime(2025, 9, 25, 17, 35, 25, 553, DateTimeKind.Utc).AddTicks(781) });

            migrationBuilder.UpdateData(
                table: "MappingTables",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 25, 17, 35, 25, 553, DateTimeKind.Utc).AddTicks(784), new DateTime(2025, 9, 25, 17, 35, 25, 553, DateTimeKind.Utc).AddTicks(784) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Customers");

            migrationBuilder.UpdateData(
                table: "MappingTables",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 25, 17, 21, 54, 951, DateTimeKind.Utc).AddTicks(2152), new DateTime(2025, 9, 25, 17, 21, 54, 951, DateTimeKind.Utc).AddTicks(2153) });

            migrationBuilder.UpdateData(
                table: "MappingTables",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 25, 17, 21, 54, 951, DateTimeKind.Utc).AddTicks(2155), new DateTime(2025, 9, 25, 17, 21, 54, 951, DateTimeKind.Utc).AddTicks(2156) });
        }
    }
}
