using Microsoft.EntityFrameworkCore.Migrations;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260521142000_BackfillAspNetUsersExtendedFields")]
    public partial class BackfillAspNetUsersExtendedFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[AspNetUsers]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('dbo.AspNetUsers', 'LastModified') IS NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [LastModified] datetime2 NULL;
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'ModifiedAt') IS NOT NULL
                       AND COL_LENGTH('dbo.AspNetUsers', 'LastModified') IS NOT NULL
                    BEGIN
                        EXEC(N'UPDATE [AspNetUsers] SET [LastModified] = COALESCE([LastModified], [ModifiedAt]);');
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'ActiveDuration') IS NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [ActiveDuration] time NOT NULL CONSTRAINT [DF_AspNetUsers_ActiveDuration] DEFAULT ('00:00:00');
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'AutoBackupEnabled') IS NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [AutoBackupEnabled] bit NOT NULL CONSTRAINT [DF_AspNetUsers_AutoBackupEnabled] DEFAULT (0);
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'CreatedBy') IS NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [CreatedBy] nvarchar(100) NULL;
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'LastModifiedBy') IS NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [LastModifiedBy] nvarchar(100) NULL;
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'NationalId') IS NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [NationalId] nvarchar(20) NULL;
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'PersonnelCode') IS NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [PersonnelCode] nvarchar(50) NULL;
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'LastLoginAt') IS NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [LastLoginAt] datetime2 NULL;
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'LastPasswordResetRequestedAt') IS NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [LastPasswordResetRequestedAt] datetime2 NULL;
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'IsActive') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_IsActive' AND object_id = OBJECT_ID(N'[AspNetUsers]'))
                    BEGIN
                        EXEC(N'CREATE INDEX [IX_Users_IsActive] ON [AspNetUsers]([IsActive]);');
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'NationalId') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_NationalId' AND object_id = OBJECT_ID(N'[AspNetUsers]'))
                    BEGIN
                        EXEC(N'CREATE UNIQUE INDEX [IX_Users_NationalId] ON [AspNetUsers]([NationalId]) WHERE [NationalId] IS NOT NULL;');
                    END;

                    IF COL_LENGTH('dbo.AspNetUsers', 'PersonnelCode') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_PersonnelCode' AND object_id = OBJECT_ID(N'[AspNetUsers]'))
                    BEGIN
                        EXEC(N'CREATE UNIQUE INDEX [IX_Users_PersonnelCode] ON [AspNetUsers]([PersonnelCode]) WHERE [PersonnelCode] IS NOT NULL;');
                    END;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[AspNetUsers]', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_NationalId' AND object_id = OBJECT_ID(N'[AspNetUsers]'))
                    BEGIN
                        DROP INDEX [IX_Users_NationalId] ON [AspNetUsers];
                    END;

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_PersonnelCode' AND object_id = OBJECT_ID(N'[AspNetUsers]'))
                    BEGIN
                        DROP INDEX [IX_Users_PersonnelCode] ON [AspNetUsers];
                    END;

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_IsActive' AND object_id = OBJECT_ID(N'[AspNetUsers]'))
                    BEGIN
                        DROP INDEX [IX_Users_IsActive] ON [AspNetUsers];
                    END;

                    IF COL_LENGTH('AspNetUsers', 'PersonnelCode') IS NOT NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] DROP COLUMN [PersonnelCode];
                    END;

                    IF COL_LENGTH('AspNetUsers', 'NationalId') IS NOT NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] DROP COLUMN [NationalId];
                    END;

                    IF COL_LENGTH('AspNetUsers', 'LastModifiedBy') IS NOT NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] DROP COLUMN [LastModifiedBy];
                    END;

                    IF COL_LENGTH('AspNetUsers', 'CreatedBy') IS NOT NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] DROP COLUMN [CreatedBy];
                    END;

                    IF COL_LENGTH('AspNetUsers', 'AutoBackupEnabled') IS NOT NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] DROP CONSTRAINT [DF_AspNetUsers_AutoBackupEnabled];
                        ALTER TABLE [AspNetUsers] DROP COLUMN [AutoBackupEnabled];
                    END;

                    IF COL_LENGTH('AspNetUsers', 'ActiveDuration') IS NOT NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] DROP CONSTRAINT [DF_AspNetUsers_ActiveDuration];
                        ALTER TABLE [AspNetUsers] DROP COLUMN [ActiveDuration];
                    END;

                    IF COL_LENGTH('AspNetUsers', 'LastPasswordResetRequestedAt') IS NOT NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] DROP COLUMN [LastPasswordResetRequestedAt];
                    END;

                    IF COL_LENGTH('AspNetUsers', 'LastLoginAt') IS NOT NULL
                    BEGIN
                        ALTER TABLE [AspNetUsers] DROP COLUMN [LastLoginAt];
                    END;
                END;
                """);
        }
    }
}
