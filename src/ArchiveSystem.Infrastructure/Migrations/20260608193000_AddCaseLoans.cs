using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260608193000_AddCaseLoans")]
    public partial class AddCaseLoans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
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
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[CaseLoans]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [CaseLoans];
                END;
                """);
        }
    }
}
