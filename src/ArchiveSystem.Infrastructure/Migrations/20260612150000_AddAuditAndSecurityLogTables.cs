using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260612150000_AddAuditAndSecurityLogTables")]
    public partial class AddAuditAndSecurityLogTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Logs_AuditTrail]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Logs_AuditTrail](
                        [AuditId] bigint IDENTITY(1,1) NOT NULL,
                        [EventType] nvarchar(200) NOT NULL,
                        [TableName] nvarchar(128) NOT NULL,
                        [RecordId] nvarchar(128) NOT NULL,
                        [UserId] nvarchar(64) NULL,
                        [ActionDate] datetime2 NOT NULL,
                        [OldValues] nvarchar(max) NULL,
                        [NewValues] nvarchar(max) NULL,
                        [AffectedColumns] nvarchar(max) NULL,
                        CONSTRAINT [PK_Logs_AuditTrail] PRIMARY KEY ([AuditId])
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Logs_AuditTrail_ActionDate' AND object_id = OBJECT_ID(N'[dbo].[Logs_AuditTrail]'))
                    CREATE INDEX [IX_Logs_AuditTrail_ActionDate] ON [dbo].[Logs_AuditTrail]([ActionDate]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Logs_AuditTrail_UserId_ActionDate' AND object_id = OBJECT_ID(N'[dbo].[Logs_AuditTrail]'))
                    CREATE INDEX [IX_Logs_AuditTrail_UserId_ActionDate] ON [dbo].[Logs_AuditTrail]([UserId], [ActionDate]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Logs_AuditTrail_TableName_ActionDate' AND object_id = OBJECT_ID(N'[dbo].[Logs_AuditTrail]'))
                    CREATE INDEX [IX_Logs_AuditTrail_TableName_ActionDate] ON [dbo].[Logs_AuditTrail]([TableName], [ActionDate]);

                IF OBJECT_ID(N'[dbo].[Logs_Security]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Logs_Security](
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [Message] nvarchar(max) NULL,
                        [MessageTemplate] nvarchar(max) NULL,
                        [Level] nvarchar(128) NULL,
                        [TimeStamp] datetime2 NOT NULL,
                        [Exception] nvarchar(max) NULL,
                        [LogEvent] nvarchar(max) NULL,
                        [UserId] nvarchar(64) NULL,
                        [ClientIpAddress] nvarchar(64) NULL,
                        [MachineName] nvarchar(128) NULL,
                        [LogCategory] nvarchar(32) NULL,
                        [EventType] nvarchar(120) NULL,
                        [ActorUserId] nvarchar(64) NULL,
                        [TargetUserId] nvarchar(64) NULL,
                        [SecurityMetadata] nvarchar(max) NULL,
                        CONSTRAINT [PK_Logs_Security] PRIMARY KEY ([Id])
                    );
                END;

                IF COL_LENGTH(N'dbo.Logs_Security', N'EventType') IS NULL
                    ALTER TABLE [dbo].[Logs_Security] ADD [EventType] nvarchar(120) NULL;
                IF COL_LENGTH(N'dbo.Logs_Security', N'ActorUserId') IS NULL
                    ALTER TABLE [dbo].[Logs_Security] ADD [ActorUserId] nvarchar(64) NULL;
                IF COL_LENGTH(N'dbo.Logs_Security', N'TargetUserId') IS NULL
                    ALTER TABLE [dbo].[Logs_Security] ADD [TargetUserId] nvarchar(64) NULL;
                IF COL_LENGTH(N'dbo.Logs_Security', N'ClientIpAddress') IS NULL
                    ALTER TABLE [dbo].[Logs_Security] ADD [ClientIpAddress] nvarchar(64) NULL;
                IF COL_LENGTH(N'dbo.Logs_Security', N'SecurityMetadata') IS NULL
                    ALTER TABLE [dbo].[Logs_Security] ADD [SecurityMetadata] nvarchar(max) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Logs_Security_ActionDate' AND object_id = OBJECT_ID(N'[dbo].[Logs_Security]'))
                    CREATE INDEX [IX_Logs_Security_ActionDate] ON [dbo].[Logs_Security]([TimeStamp]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Logs_Security_ActorUserId_ActionDate' AND object_id = OBJECT_ID(N'[dbo].[Logs_Security]'))
                    CREATE INDEX [IX_Logs_Security_ActorUserId_ActionDate] ON [dbo].[Logs_Security]([ActorUserId], [TimeStamp]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Logs_Security_TargetUserId_ActionDate' AND object_id = OBJECT_ID(N'[dbo].[Logs_Security]'))
                    CREATE INDEX [IX_Logs_Security_TargetUserId_ActionDate] ON [dbo].[Logs_Security]([TargetUserId], [TimeStamp]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Logs_Security_EventType_ActionDate' AND object_id = OBJECT_ID(N'[dbo].[Logs_Security]'))
                    CREATE INDEX [IX_Logs_Security_EventType_ActionDate] ON [dbo].[Logs_Security]([EventType], [TimeStamp]);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Logs_AuditTrail]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Logs_AuditTrail];
                IF OBJECT_ID(N'[dbo].[Logs_Security]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Logs_Security];
                """);
        }
    }
}
