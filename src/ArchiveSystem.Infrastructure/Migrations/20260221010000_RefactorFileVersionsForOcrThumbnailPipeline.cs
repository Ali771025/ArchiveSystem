using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    public partial class RefactorFileVersionsForOcrThumbnailPipeline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('FileVersions', 'ObjectKey') IS NOT NULL AND COL_LENGTH('FileVersions', 'StoragePath') IS NULL
                BEGIN
                    EXEC sp_rename 'FileVersions.ObjectKey', 'StoragePath', 'COLUMN';
                END;

                IF COL_LENGTH('FileVersions', 'OriginalFileName') IS NOT NULL AND COL_LENGTH('FileVersions', 'FileName') IS NULL
                BEGIN
                    EXEC sp_rename 'FileVersions.OriginalFileName', 'FileName', 'COLUMN';
                END;

                IF COL_LENGTH('FileVersions', 'SizeBytes') IS NOT NULL AND COL_LENGTH('FileVersions', 'Size') IS NULL
                BEGIN
                    EXEC sp_rename 'FileVersions.SizeBytes', 'Size', 'COLUMN';
                END;

                IF COL_LENGTH('FileVersions', 'Sha256Checksum') IS NOT NULL AND COL_LENGTH('FileVersions', 'Sha256') IS NULL
                BEGIN
                    EXEC sp_rename 'FileVersions.Sha256Checksum', 'Sha256', 'COLUMN';
                END;

                IF COL_LENGTH('FileVersions', 'ThumbnailRequested') IS NOT NULL
                BEGIN
                    ALTER TABLE [FileVersions] DROP COLUMN [ThumbnailRequested];
                END;

                IF COL_LENGTH('FileVersions', 'OcrRequested') IS NOT NULL
                BEGIN
                    ALTER TABLE [FileVersions] DROP COLUMN [OcrRequested];
                END;

                IF COL_LENGTH('FileVersions', 'Thumbnail') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [Thumbnail] varbinary(max) NULL;
                END;

                IF COL_LENGTH('FileVersions', 'ThumbnailContentType') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [ThumbnailContentType] nvarchar(128) NULL;
                END;

                IF COL_LENGTH('FileVersions', 'ThumbnailWidth') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [ThumbnailWidth] int NULL;
                END;

                IF COL_LENGTH('FileVersions', 'ThumbnailHeight') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [ThumbnailHeight] int NULL;
                END;

                IF COL_LENGTH('FileVersions', 'ThumbnailStatus') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [ThumbnailStatus] int NOT NULL CONSTRAINT [DF_FileVersions_ThumbnailStatus] DEFAULT(0);
                END;

                IF COL_LENGTH('FileVersions', 'ThumbnailError') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [ThumbnailError] nvarchar(512) NULL;
                END;

                IF COL_LENGTH('FileVersions', 'OcrResult') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [OcrResult] nvarchar(max) NULL;
                END;

                IF COL_LENGTH('FileVersions', 'OcrStatus') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [OcrStatus] int NOT NULL CONSTRAINT [DF_FileVersions_OcrStatus] DEFAULT(0);
                END;

                IF COL_LENGTH('FileVersions', 'OcrError') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [OcrError] nvarchar(512) NULL;
                END;

                IF COL_LENGTH('FileVersions', 'OcrLanguage') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [OcrLanguage] nvarchar(32) NULL;
                END;

                IF COL_LENGTH('FileVersions', 'OcrWordCount') IS NULL
                BEGIN
                    ALTER TABLE [FileVersions] ADD [OcrWordCount] int NULL;
                END;

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_ObjectKey' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    DROP INDEX [IX_FileVersions_ObjectKey] ON [FileVersions];
                END;

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_Sha256Checksum' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    DROP INDEX [IX_FileVersions_Sha256Checksum] ON [FileVersions];
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_StoragePath' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    CREATE INDEX [IX_FileVersions_StoragePath] ON [FileVersions]([StoragePath]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_Sha256' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    CREATE INDEX [IX_FileVersions_Sha256] ON [FileVersions]([Sha256]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_ThumbnailStatus_CreatedAt' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    CREATE INDEX [IX_FileVersions_ThumbnailStatus_CreatedAt] ON [FileVersions]([ThumbnailStatus], [CreatedAt]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_OcrStatus_CreatedAt' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    CREATE INDEX [IX_FileVersions_OcrStatus_CreatedAt] ON [FileVersions]([OcrStatus], [CreatedAt]);
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_ThumbnailStatus_CreatedAt' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    DROP INDEX [IX_FileVersions_ThumbnailStatus_CreatedAt] ON [FileVersions];
                END;

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_OcrStatus_CreatedAt' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    DROP INDEX [IX_FileVersions_OcrStatus_CreatedAt] ON [FileVersions];
                END;

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_StoragePath' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    DROP INDEX [IX_FileVersions_StoragePath] ON [FileVersions];
                END;

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_Sha256' AND object_id = OBJECT_ID('FileVersions'))
                BEGIN
                    DROP INDEX [IX_FileVersions_Sha256] ON [FileVersions];
                END;
                """);
        }
    }
}
