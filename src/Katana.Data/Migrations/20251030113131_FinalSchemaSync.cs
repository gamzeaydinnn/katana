using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalSchemaSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure the PendingStockAdjustments table exists (some environments had migration history but missing table).
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[PendingStockAdjustments]') IS NULL
BEGIN
    CREATE TABLE [dbo].[PendingStockAdjustments] (
        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ExternalOrderId] nvarchar(100) NOT NULL,
        [ProductId] int NOT NULL,
        [Sku] nvarchar(100) NULL,
        [Quantity] int NOT NULL,
        [RequestedBy] nvarchar(100) NOT NULL,
        [RequestedAt] datetimeoffset NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ApprovedBy] nvarchar(max) NULL,
        [ApprovedAt] datetimeoffset NULL,
        [RejectionReason] nvarchar(500) NULL,
        [Notes] nvarchar(1000) NULL
    );
    CREATE UNIQUE INDEX IX_PendingStockAdjustments_ExternalOrderId ON [dbo].[PendingStockAdjustments]([ExternalOrderId]);
    CREATE INDEX IX_PendingStockAdjustments_RequestedAt ON [dbo].[PendingStockAdjustments]([RequestedAt]);
    CREATE INDEX IX_PendingStockAdjustments_Status ON [dbo].[PendingStockAdjustments]([Status]);
END
");

            // If the table exists but ProductId was bigint, alter to int
            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "PendingStockAdjustments",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "ProductId",
                table: "PendingStockAdjustments",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
