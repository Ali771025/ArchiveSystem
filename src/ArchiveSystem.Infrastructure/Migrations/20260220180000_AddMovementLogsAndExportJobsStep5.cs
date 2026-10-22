using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    public partial class AddMovementLogsAndExportJobsStep5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: true),
                    RequestJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResultObjectKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovementLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: false),
                    ActorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClassificationId = table.Column<long>(type: "bigint", nullable: true),
                    CaseId = table.Column<long>(type: "bigint", nullable: true),
                    FileId = table.Column<long>(type: "bigint", nullable: true),
                    FileVersionId = table.Column<long>(type: "bigint", nullable: true),
                    TagId = table.Column<long>(type: "bigint", nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovementLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_CreatedByUserId_CreatedAt",
                table: "ExportJobs",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_Status_CreatedAt",
                table: "ExportJobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementLogs_ActionType_Timestamp",
                table: "MovementLogs",
                columns: new[] { "ActionType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementLogs_ActorUserId_Timestamp",
                table: "MovementLogs",
                columns: new[] { "ActorUserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementLogs_CaseId_Timestamp",
                table: "MovementLogs",
                columns: new[] { "CaseId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementLogs_ClassificationId_Timestamp",
                table: "MovementLogs",
                columns: new[] { "ClassificationId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementLogs_FileId_Timestamp",
                table: "MovementLogs",
                columns: new[] { "FileId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementLogs_TagId_Timestamp",
                table: "MovementLogs",
                columns: new[] { "TagId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementLogs_Timestamp",
                table: "MovementLogs",
                column: "Timestamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExportJobs");
            migrationBuilder.DropTable(name: "MovementLogs");
        }
    }
}
