using Microsoft.EntityFrameworkCore.Migrations;

// ReSharper disable InconsistentNaming
namespace Katana.Data.Migrations
{
    public partial class AddLogsIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ErrorLogs: composite index Level + CreatedAt (supports filtering by level and keyset range)
            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Level_CreatedAt",
                table: "ErrorLogs",
                columns: new[] { "Level", "CreatedAt" }
            );

            // AuditLogs: composite index EntityName + ActionType + Timestamp
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_ActionType_Timestamp",
                table: "AuditLogs",
                columns: new[] { "EntityName", "ActionType", "Timestamp" }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_Level_CreatedAt",
                table: "ErrorLogs"
            );

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityName_ActionType_Timestamp",
                table: "AuditLogs"
            );
        }
    }
}

