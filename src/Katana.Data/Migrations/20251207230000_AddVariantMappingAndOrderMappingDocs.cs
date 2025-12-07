using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations;

public partial class AddVariantMappingAndOrderMappingDocs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
            name: "IX_VariantMappings_KatanaVariantId",
            table: "VariantMappings",
            column: "KatanaVariantId",
            unique: true);

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
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BelgeNo",
            table: "OrderMappings");

        migrationBuilder.DropColumn(
            name: "BelgeSeri",
            table: "OrderMappings");

        migrationBuilder.DropColumn(
            name: "BelgeTakipNo",
            table: "OrderMappings");

        migrationBuilder.DropTable(
            name: "VariantMappings");
    }
}
