using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260609193000_AddSqlFileStreamBlobsTable")]
    public partial class AddSqlFileStreamBlobsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[FileBlobs]', N'U') IS NULL
                BEGIN
                    DECLARE @filestreamFilegroup sysname;
                    SELECT TOP 1 @filestreamFilegroup = fg.name
                    FROM sys.filegroups fg
                    WHERE fg.type = 'FD';

                    IF @filestreamFilegroup IS NOT NULL
                    BEGIN
                        DECLARE @sql nvarchar(max) = N'
                            CREATE TABLE [FileBlobs](
                                [BlobKey] nvarchar(1000) NOT NULL,
                                [BlobId] uniqueidentifier ROWGUIDCOL NOT NULL UNIQUE DEFAULT NEWSEQUENTIALID(),
                                [Content] varbinary(max) FILESTREAM NULL,
                                [ContentType] nvarchar(256) NULL,
                                [CreatedAt] datetime2 NOT NULL,
                                [UpdatedAt] datetime2 NOT NULL,
                                CONSTRAINT [PK_FileBlobs] PRIMARY KEY ([BlobKey])
                            ) FILESTREAM_ON [' + @filestreamFilegroup + N'];';
                        EXEC sp_executesql @sql;
                    END
                    ELSE
                    BEGIN
                        CREATE TABLE [FileBlobs](
                            [BlobKey] nvarchar(1000) NOT NULL,
                            [BlobId] uniqueidentifier ROWGUIDCOL NOT NULL UNIQUE DEFAULT NEWSEQUENTIALID(),
                            [Content] varbinary(max) NULL,
                            [ContentType] nvarchar(256) NULL,
                            [CreatedAt] datetime2 NOT NULL,
                            [UpdatedAt] datetime2 NOT NULL,
                            CONSTRAINT [PK_FileBlobs] PRIMARY KEY ([BlobKey])
                        );
                    END
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileBlobs_UpdatedAt' AND object_id = OBJECT_ID('[FileBlobs]'))
                BEGIN
                    CREATE INDEX [IX_FileBlobs_UpdatedAt] ON [FileBlobs]([UpdatedAt]);
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[FileBlobs]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [FileBlobs];
                END;
                """);
        }
    }
}
