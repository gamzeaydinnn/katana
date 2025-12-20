using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Katana.Data.Migrations;

public partial class CreateLucaProducts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LucaProducts",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                LucaCode = table.Column<string>(maxLength: 100, nullable: false),
                LucaName = table.Column<string>(maxLength: 300, nullable: false),
                LucaCategory = table.Column<string>(maxLength: 200, nullable: true),
                UpdatedAt = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LucaProducts", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LucaProducts");
    }
}
