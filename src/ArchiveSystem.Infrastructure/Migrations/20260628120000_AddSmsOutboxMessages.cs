using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260628120000_AddSmsOutboxMessages")]
    public partial class AddSmsOutboxMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[SmsOutboxMessages]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SmsOutboxMessages] (
                        [Id] uniqueidentifier NOT NULL,
                        [UserId] bigint NOT NULL,
                        [MobileNumber] nvarchar(20) NOT NULL,
                        [Purpose] nvarchar(40) NOT NULL,
                        [Provider] nvarchar(64) NOT NULL,
                        [PatternCode] nvarchar(120) NOT NULL,
                        [PayloadJson] nvarchar(max) NOT NULL,
                        [CorrelationId] nvarchar(100) NOT NULL,
                        [ClientIpAddress] nvarchar(64) NULL,
                        [UserAgent] nvarchar(512) NULL,
                        [Status] int NOT NULL,
                        [AttemptCount] int NOT NULL,
                        [MaxAttempts] int NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [SentAtUtc] datetime2 NULL,
                        [NextAttemptAtUtc] datetime2 NULL,
                        [LastError] nvarchar(1000) NULL,
                        CONSTRAINT [PK_SmsOutboxMessages] PRIMARY KEY ([Id])
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SmsOutboxMessages_Status_NextAttemptAtUtc' AND object_id = OBJECT_ID('[SmsOutboxMessages]'))
                BEGIN
                    CREATE INDEX [IX_SmsOutboxMessages_Status_NextAttemptAtUtc] ON [SmsOutboxMessages] ([Status], [NextAttemptAtUtc]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SmsOutboxMessages_CreatedAtUtc' AND object_id = OBJECT_ID('[SmsOutboxMessages]'))
                BEGIN
                    CREATE INDEX [IX_SmsOutboxMessages_CreatedAtUtc] ON [SmsOutboxMessages] ([CreatedAtUtc]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SmsOutboxMessages_MobileNumber' AND object_id = OBJECT_ID('[SmsOutboxMessages]'))
                BEGIN
                    CREATE INDEX [IX_SmsOutboxMessages_MobileNumber] ON [SmsOutboxMessages] ([MobileNumber]);
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[SmsOutboxMessages]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [SmsOutboxMessages];
                END;
                """);
        }
    }
}
