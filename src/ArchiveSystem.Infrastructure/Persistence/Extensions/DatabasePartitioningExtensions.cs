// src/ArchiveSystem.Infrastructure/Persistence/Extensions/DatabasePartitioningExtensions.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace ArchiveSystem.Infrastructure.Persistence.Extensions
{
    public static class DatabasePartitioningExtensions
    {
        /// <summary>
        /// Creates partitioning infrastructure for Files table with monthly boundaries.
        /// Includes 24 months of future partitions (configurable) and proper filegroup alignment.
        /// </summary>
        public static void CreateFilePartitioning(MigrationBuilder migrationBuilder, int futureMonths = 24)
        {
            // Generate partition boundaries from 2024-01-01 through current date + futureMonths
            var startDate = new DateTime(2024, 1, 1);
            var endDate = DateTime.UtcNow.Date.AddMonths(futureMonths + 1);
            var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
            var boundaries = new List<string>();

            while (currentDate < endDate)
            {
                boundaries.Add($"'{currentDate:yyyy-MM-dd}'");
                currentDate = currentDate.AddMonths(1);
            }

            var boundaryValues = string.Join(", ", boundaries);

            // 1. Create dedicated filegroups for partitions (run once during DB setup)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT name FROM sys.filegroups WHERE name = N'FG_Archive_History')
                    ALTER DATABASE CURRENT ADD FILEGROUP FG_Archive_History;
                IF NOT EXISTS (SELECT name FROM sys.filegroups WHERE name = N'FG_Archive_Current')
                    ALTER DATABASE CURRENT ADD FILEGROUP FG_Archive_Current;
                IF NOT EXISTS (SELECT name FROM sys.filegroups WHERE name = N'FG_Archive_Future')
                    ALTER DATABASE CURRENT ADD FILEGROUP FG_Archive_Future;
                
                -- Add files to filegroups (adjust paths per environment)
                DECLARE @DataPath NVARCHAR(512) = (SELECT LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name))) 
                                                  FROM sys.database_files WHERE type_desc = 'ROWS');
                
                IF NOT EXISTS (SELECT * FROM sys.database_files WHERE name = 'Archive_History')
                    ALTER DATABASE CURRENT ADD FILE (NAME = 'Archive_History', FILENAME = @DataPath + '\Archive_History.ndf', SIZE = 100MB, MAXSIZE = UNLIMITED, FILEGROWTH = 100MB) TO FILEGROUP FG_Archive_History;
                IF NOT EXISTS (SELECT * FROM sys.database_files WHERE name = 'Archive_Current')
                    ALTER DATABASE CURRENT ADD FILE (NAME = 'Archive_Current', FILENAME = @DataPath + '\Archive_Current.ndf', SIZE = 250MB, MAXSIZE = UNLIMITED, FILEGROWTH = 250MB) TO FILEGROUP FG_Archive_Current;
                IF NOT EXISTS (SELECT * FROM sys.database_files WHERE name = 'Archive_Future')
                    ALTER DATABASE CURRENT ADD FILE (NAME = 'Archive_Future', FILENAME = @DataPath + '\Archive_Future.ndf', SIZE = 100MB, MAXSIZE = UNLIMITED, FILEGROWTH = 100MB) TO FILEGROUP FG_Archive_Future;
            ");

            // 2. Create partition function with dynamic boundaries
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT * FROM sys.partition_functions WHERE name = 'FilePartitionFunction')
                BEGIN
                    CREATE PARTITION FUNCTION FilePartitionFunction (datetime2)
                    AS RANGE RIGHT FOR VALUES ({boundaryValues});
                END
                ELSE
                BEGIN
                    -- Add missing boundaries if function exists
                    DECLARE @maxBoundary datetime2;
                    SELECT @maxBoundary = MAX(value) 
                    FROM sys.partition_range_values prv
                    INNER JOIN sys.partition_functions pf ON prv.function_id = pf.function_id
                    WHERE pf.name = 'FilePartitionFunction';

                    {string.Join("\n", boundaries.Select(b => 
                        $"IF @maxBoundary < {b} ALTER PARTITION FUNCTION FilePartitionFunction() SPLIT RANGE ({b});"))}
                END
            ");

            // 3. Create partition scheme with proper filegroup distribution
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.partition_schemes WHERE name = 'FilePartitionScheme')
                BEGIN
                    CREATE PARTITION SCHEME FilePartitionScheme
                    AS PARTITION FilePartitionFunction
                    TO ([FG_Archive_History], [FG_Archive_History], [FG_Archive_History], [FG_Archive_History],
                        [FG_Archive_History], [FG_Archive_History], [FG_Archive_History], [FG_Archive_History],
                        [FG_Archive_History], [FG_Archive_History], [FG_Archive_History], [FG_Archive_History],
                        [FG_Archive_History], [FG_Archive_History], [FG_Archive_History], [FG_Archive_History],
                        [FG_Archive_History], [FG_Archive_History], [FG_Archive_History], [FG_Archive_History],
                        [FG_Archive_History], [FG_Archive_History], [FG_Archive_History], [FG_Archive_History],
                        [FG_Archive_Current], [FG_Archive_Current], [FG_Archive_Current], [FG_Archive_Current],
                        [FG_Archive_Future], [FG_Archive_Future], [FG_Archive_Future], [FG_Archive_Future]);
                END
            ");

            // 4. Apply partitioning to Files table (requires clustered index rebuild)
            migrationBuilder.Sql(@"
                -- Ensure table has partition-aligned clustered index
                IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Files') AND name = 'PK_Files' AND is_primary_key = 1)
                BEGIN
                    -- Drop existing PK if not partitioned
                    ALTER TABLE dbo.Files DROP CONSTRAINT PK_Files;
                END

                -- Recreate PK with partition alignment on CreatedAt
                ALTER TABLE dbo.Files 
                ADD CONSTRAINT PK_Files PRIMARY KEY CLUSTERED (Id, CreatedAt)
                ON FilePartitionScheme(CreatedAt);
            ");
        }

        /// <summary>
        /// Adds new partition boundary for next month (run monthly via maintenance job)
        /// </summary>
        public static void AddNextMonthPartition(MigrationBuilder migrationBuilder)
        {
            var nextMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
            migrationBuilder.Sql($@"
                DECLARE @boundary datetime2 = '{nextMonth:yyyy-MM-dd}';
                IF NOT EXISTS (
                    SELECT 1 FROM sys.partition_range_values prv
                    INNER JOIN sys.partition_functions pf ON prv.function_id = pf.function_id
                    WHERE pf.name = 'FilePartitionFunction' AND prv.value = @boundary
                )
                BEGIN
                    ALTER PARTITION SCHEME FilePartitionScheme NEXT USED FG_Archive_Future;
                    ALTER PARTITION FUNCTION FilePartitionFunction() SPLIT RANGE (@boundary);
                END
            ");
        }

        /// <summary>
        /// Archives old partition by switching to archive table (run quarterly)
        /// </summary>
        public static void ArchiveOldPartition(MigrationBuilder migrationBuilder, DateTime archiveBeforeDate)
        {
            migrationBuilder.Sql($@"
                -- Create staging table with same schema
                IF OBJECT_ID('dbo.Files_ArchiveStaging') IS NOT NULL
                    DROP TABLE dbo.Files_ArchiveStaging;

                SELECT TOP 0 * INTO dbo.Files_ArchiveStaging FROM dbo.Files;
                CREATE CLUSTERED INDEX IX_ArchiveStaging ON dbo.Files_ArchiveStaging(Id, CreatedAt) 
                    ON FG_Archive_History;

                -- Switch out oldest partition
                ALTER TABLE dbo.Files SWITCH PARTITION 1 TO dbo.Files_ArchiveStaging PARTITION 1;

                -- Merge empty partition boundary
                ALTER PARTITION FUNCTION FilePartitionFunction() MERGE RANGE ('{archiveBeforeDate:yyyy-MM-dd}');
            ");
        }
    }
}