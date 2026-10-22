using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Infrastructure.Services;

public sealed class DatabaseStartupService : IDatabaseStartupService
{
    private readonly ArchiveDbContext _dbContext;
    private readonly ILogger<DatabaseStartupService> _logger;

    public DatabaseStartupService(
        ArchiveDbContext dbContext,
        ILogger<DatabaseStartupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Database.MigrateAsync(cancellationToken);
    }

    public async Task EnsureSchemaCompatibilityAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('Cases', 'AvatarImagePath') IS NULL
            BEGIN
                ALTER TABLE [Cases] ADD [AvatarImagePath] nvarchar(500) NULL;
            END;
            """,
            cancellationToken);

        await _dbContext.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('DynamicFieldDefinitions', 'ShowAsFilter') IS NULL
            BEGIN
                ALTER TABLE [DynamicFieldDefinitions]
                ADD [ShowAsFilter] bit NOT NULL
                    CONSTRAINT [DF_DynamicFieldDefinitions_ShowAsFilter] DEFAULT(0);
            END;

            IF COL_LENGTH('DynamicFieldDefinitions', 'FilterType') IS NULL
            BEGIN
                ALTER TABLE [DynamicFieldDefinitions]
                ADD [FilterType] int NOT NULL
                    CONSTRAINT [DF_DynamicFieldDefinitions_FilterType] DEFAULT(0);
            END;

            IF COL_LENGTH('Cases', 'PostalCode') IS NULL
            BEGIN
                ALTER TABLE [Cases] ADD [PostalCode] nvarchar(10) NULL;
            END;

            IF COL_LENGTH('Cases', 'ExternalCaseNumber') IS NULL
            BEGIN
                ALTER TABLE [Cases] ADD [ExternalCaseNumber] nvarchar(100) NULL;
                UPDATE [Cases] SET [ExternalCaseNumber] = [CaseNumber];
                ALTER TABLE [Cases] ALTER COLUMN [ExternalCaseNumber] nvarchar(100) NOT NULL;
            END;

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_Cases_ExternalCaseNumber'
                  AND object_id = OBJECT_ID(N'[Cases]')
            )
            BEGIN
                CREATE UNIQUE INDEX [IX_Cases_ExternalCaseNumber]
                    ON [Cases]([ExternalCaseNumber]);
            END;

            IF COL_LENGTH('Owners', 'TaxpayerId') IS NULL
            BEGIN
                ALTER TABLE [Owners] ADD [TaxpayerId] nvarchar(14) NULL;
            END;

            IF COL_LENGTH('AspNetUsers', 'PhoneNumber') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    WHERE c.object_id = OBJECT_ID(N'[AspNetUsers]')
                      AND c.name = N'PhoneNumber'
                      AND c.system_type_id = 231 -- nvarchar
                      AND c.max_length = -1       -- (max)
                )
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [PhoneNumber] IS NOT NULL AND LEN([PhoneNumber]) > 20)
                    BEGIN
                        ALTER TABLE [AspNetUsers] ALTER COLUMN [PhoneNumber] nvarchar(20) NULL;
                    END;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Users_PhoneNumber'
                      AND object_id = OBJECT_ID(N'[AspNetUsers]')
                )
                AND EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    WHERE c.object_id = OBJECT_ID(N'[AspNetUsers]')
                      AND c.name = N'PhoneNumber'
                      AND c.system_type_id IN (167, 175, 231, 239) -- varchar,char,nvarchar,nchar
                      AND c.max_length > 0
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Users_PhoneNumber] ON [AspNetUsers]([PhoneNumber]) WHERE [PhoneNumber] IS NOT NULL;
                END;
            END;
            """,
            cancellationToken);

        await _dbContext.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Taxpayers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Taxpayers](
                    [Id] uniqueidentifier NOT NULL,
                    [TaxpayerType] int NOT NULL,
                    [FirstName] nvarchar(100) NOT NULL,
                    [LastName] nvarchar(100) NOT NULL,
                    [NationalId] nvarchar(10) NOT NULL,
                    [IdentityNumber] nvarchar(50) NULL,
                    [Mobile] nvarchar(20) NULL,
                    [LandlinePhone] nvarchar(20) NULL,
                    [PostalCode] nvarchar(10) NULL,
                    [Address] nvarchar(1000) NULL,
                    [DeletedAt] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [ModifiedAt] datetime2 NULL,
                    CONSTRAINT [PK_Taxpayers] PRIMARY KEY ([Id])
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_FirstName' AND object_id = OBJECT_ID('[Taxpayers]'))
            BEGIN
                CREATE INDEX [IX_Taxpayers_FirstName] ON [Taxpayers]([FirstName]);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_LastName' AND object_id = OBJECT_ID('[Taxpayers]'))
            BEGIN
                CREATE INDEX [IX_Taxpayers_LastName] ON [Taxpayers]([LastName]);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_NationalId' AND object_id = OBJECT_ID('[Taxpayers]'))
               AND NOT EXISTS (
                   SELECT [NationalId]
                   FROM [Taxpayers]
                   WHERE [DeletedAt] IS NULL
                   GROUP BY [NationalId]
                   HAVING COUNT(1) > 1
               )
            BEGIN
                CREATE UNIQUE INDEX [IX_Taxpayers_NationalId] ON [Taxpayers]([NationalId]) WHERE [DeletedAt] IS NULL;
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_Mobile' AND object_id = OBJECT_ID('[Taxpayers]'))
               AND NOT EXISTS (
                   SELECT [Mobile]
                   FROM [Taxpayers]
                   WHERE [Mobile] IS NOT NULL
                     AND [DeletedAt] IS NULL
                   GROUP BY [Mobile]
                   HAVING COUNT(1) > 1
               )
            BEGIN
                CREATE UNIQUE INDEX [IX_Taxpayers_Mobile] ON [Taxpayers]([Mobile]) WHERE [Mobile] IS NOT NULL AND [DeletedAt] IS NULL;
            END;

            IF OBJECT_ID(N'[CaseLoans]', N'U') IS NULL
            BEGIN
                CREATE TABLE [CaseLoans](
                    [Id] uniqueidentifier NOT NULL,
                    [BorrowerFirstName] nvarchar(100) NOT NULL,
                    [BorrowerLastName] nvarchar(100) NOT NULL,
                    [CaseNumber] nvarchar(50) NOT NULL,
                    [NationalId] nvarchar(10) NOT NULL,
                    [ContactNumber] nvarchar(20) NOT NULL,
                    [CheckoutDate] datetime2 NOT NULL,
                    [ExpectedReturnDate] datetime2 NULL,
                    [ActualReturnDate] datetime2 NULL,
                    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_CaseLoans_Status] DEFAULT (N'Loaned'),
                    [ReceiverUnit] nvarchar(150) NULL,
                    [RegisteredBy] nvarchar(150) NULL,
                    [Description] nvarchar(1000) NULL,
                    [IsReturned] bit NOT NULL CONSTRAINT [DF_CaseLoans_IsReturned] DEFAULT (0),
                    [CreatedAt] datetime2 NOT NULL,
                    [ModifiedAt] datetime2 NULL,
                    CONSTRAINT [PK_CaseLoans] PRIMARY KEY ([Id])
                );
            END;

            IF COL_LENGTH('CaseLoans', 'ActualReturnDate') IS NULL
            BEGIN
                ALTER TABLE [CaseLoans] ADD [ActualReturnDate] datetime2 NULL;
            END;

            IF COL_LENGTH('CaseLoans', 'Status') IS NULL
            BEGIN
                ALTER TABLE [CaseLoans] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_CaseLoans_Status] DEFAULT (N'Loaned');
            END;

            IF COL_LENGTH('CaseLoans', 'ReceiverUnit') IS NULL
            BEGIN
                ALTER TABLE [CaseLoans] ADD [ReceiverUnit] nvarchar(150) NULL;
            END;

            IF COL_LENGTH('CaseLoans', 'RegisteredBy') IS NULL
            BEGIN
                ALTER TABLE [CaseLoans] ADD [RegisteredBy] nvarchar(150) NULL;
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CaseLoans_CaseNumber' AND object_id = OBJECT_ID('[CaseLoans]'))
            BEGIN
                CREATE INDEX [IX_CaseLoans_CaseNumber] ON [CaseLoans]([CaseNumber]);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CaseLoans_NationalId' AND object_id = OBJECT_ID('[CaseLoans]'))
            BEGIN
                CREATE INDEX [IX_CaseLoans_NationalId] ON [CaseLoans]([NationalId]);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CaseLoans_BorrowerLastName' AND object_id = OBJECT_ID('[CaseLoans]'))
            BEGIN
                CREATE INDEX [IX_CaseLoans_BorrowerLastName] ON [CaseLoans]([BorrowerLastName]);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CaseLoans_CheckoutDate' AND object_id = OBJECT_ID('[CaseLoans]'))
            BEGIN
                CREATE INDEX [IX_CaseLoans_CheckoutDate] ON [CaseLoans]([CheckoutDate]);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CaseLoans_IsReturned' AND object_id = OBJECT_ID('[CaseLoans]'))
            BEGIN
                CREATE INDEX [IX_CaseLoans_IsReturned] ON [CaseLoans]([IsReturned]);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CaseLoans_Status' AND object_id = OBJECT_ID('[CaseLoans]'))
            BEGIN
                CREATE INDEX [IX_CaseLoans_Status] ON [CaseLoans]([Status]);
            END;
            """,
            cancellationToken);

        _logger.LogInformation("Schema compatibility checks completed.");
    }
}
