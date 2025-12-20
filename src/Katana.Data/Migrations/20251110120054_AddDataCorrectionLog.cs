using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataCorrectionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "SyncLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DataCorrectionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OriginalValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectedValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CorrectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Level_CreatedAt",
                table: "ErrorLogs",
                columns: new[] { "Level", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_ActionType_Timestamp",
                table: "AuditLogs",
                columns: new[] { "EntityName", "ActionType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionLogs_IsSynced",
                table: "DataCorrectionLogs",
                column: "IsSynced");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionLogs_SourceSystem_EntityType_IsApproved",
                table: "DataCorrectionLogs",
                columns: new[] { "SourceSystem", "EntityType", "IsApproved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataCorrectionLogs");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_Level_CreatedAt",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityName_ActionType_Timestamp",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "SyncLogs",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
