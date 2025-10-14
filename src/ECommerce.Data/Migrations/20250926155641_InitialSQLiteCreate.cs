using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSQLiteCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaxNo = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSynced = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SyncType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProcessedRecords = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulRecords = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedRecordsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TriggeredBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MappingTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MappingType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SourceValue = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TargetValue = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MappingTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SKU = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    MainImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsSynced = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FailedSyncRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IntegrationLogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RecordId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OriginalData = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FailedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastRetryAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Resolution = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedSyncRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FailedSyncRecords_IntegrationLogs_IntegrationLogId",
                        column: x => x.IntegrationLogId,
                        principalTable: "IntegrationLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsSynced = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stocks_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProductSKU = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MappingTables",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "IsActive", "MappingType", "SourceValue", "TargetValue", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 26, 15, 56, 40, 823, DateTimeKind.Utc).AddTicks(3184), null, "Default account code for unmapped products", true, "SKU_ACCOUNT", "DEFAULT", "600.01", new DateTime(2025, 9, 26, 15, 56, 40, 823, DateTimeKind.Utc).AddTicks(3185), null },
                    { 2, new DateTime(2025, 9, 26, 15, 56, 40, 823, DateTimeKind.Utc).AddTicks(3188), null, "Default warehouse code for unmapped locations", true, "LOCATION_WAREHOUSE", "DEFAULT", "MAIN", new DateTime(2025, 9, 26, 15, 56, 40, 823, DateTimeKind.Utc).AddTicks(3189), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxNo",
                table: "Customers",
                column: "TaxNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FailedSyncRecords_IntegrationLogId",
                table: "FailedSyncRecords",
                column: "IntegrationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_FailedSyncRecords_NextRetryAt",
                table: "FailedSyncRecords",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_FailedSyncRecords_RecordType_Status",
                table: "FailedSyncRecords",
                columns: new[] { "RecordType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_Status",
                table: "IntegrationLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_SyncType_StartTime",
                table: "IntegrationLogs",
                columns: new[] { "SyncType", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ProductId",
                table: "InvoiceItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNo",
                table: "Invoices",
                column: "InvoiceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IsSynced",
                table: "Invoices",
                column: "IsSynced");

            migrationBuilder.CreateIndex(
                name: "IX_MappingTables_IsActive",
                table: "MappingTables",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MappingTables_MappingType_SourceValue",
                table: "MappingTables",
                columns: new[] { "MappingType", "SourceValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_IsSynced",
                table: "Stocks",
                column: "IsSynced");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductId_Timestamp",
                table: "Stocks",
                columns: new[] { "ProductId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailedSyncRecords");

            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "MappingTables");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "IntegrationLogs");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}


