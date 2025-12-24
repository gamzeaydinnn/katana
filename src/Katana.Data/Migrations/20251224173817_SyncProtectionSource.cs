using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncProtectionSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Source kolonu zaten DB'de var (shell script ile eklendi)
            // Bu migration sadece EF model sync için
            
            // Eğer Source kolonu yoksa ekle
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Source')
                BEGIN
                    ALTER TABLE Products ADD Source NVARCHAR(50) NULL DEFAULT 'KATANA';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Products");
        }
    }
}
