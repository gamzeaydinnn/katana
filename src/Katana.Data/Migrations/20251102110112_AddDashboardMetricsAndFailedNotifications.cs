using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardMetricsAndFailedNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Hour = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    AuditCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FailedNotifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    RelatedPendingId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangeQuantity = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    SourceDocument = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WarehouseCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_CreatedAt_Level",
                table: "ErrorLogs",
                columns: new[] { "CreatedAt", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp_ActionType",
                table: "AuditLogs",
                columns: new[] { "Timestamp", "ActionType" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetrics_Hour",
                table: "DashboardMetrics",
                column: "Hour",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardMetrics");

            migrationBuilder.DropTable(
                name: "FailedNotifications");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_CreatedAt_Level",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp_ActionType",
                table: "AuditLogs");
        }
    }
}
