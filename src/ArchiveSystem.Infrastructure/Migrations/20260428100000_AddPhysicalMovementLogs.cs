using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260428100000_AddPhysicalMovementLogs")]
    public partial class AddPhysicalMovementLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[PhysicalMovementLogs]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [PhysicalMovementLogs](
                        [Id] uniqueidentifier NOT NULL,
                        [DossierNumber] nvarchar(50) NOT NULL,
                        [ReceiverFirstName] nvarchar(100) NOT NULL,
                        [ReceiverLastName] nvarchar(100) NOT NULL,
                        [NationalCode] nvarchar(10) NOT NULL,
                        [PhoneNumber] nvarchar(15) NOT NULL,
                        [CheckoutDate] datetime2 NOT NULL,
                        [ExpectedReturnDate] datetime2 NULL,
                        [Description] nvarchar(500) NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_PhysicalMovementLogs] PRIMARY KEY ([Id])
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhysicalMovementLogs_DossierNumber' AND object_id = OBJECT_ID('[PhysicalMovementLogs]'))
                BEGIN
                    CREATE INDEX [IX_PhysicalMovementLogs_DossierNumber] ON [PhysicalMovementLogs]([DossierNumber]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhysicalMovementLogs_CheckoutDate' AND object_id = OBJECT_ID('[PhysicalMovementLogs]'))
                BEGIN
                    CREATE INDEX [IX_PhysicalMovementLogs_CheckoutDate] ON [PhysicalMovementLogs]([CheckoutDate]);
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[PhysicalMovementLogs]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [PhysicalMovementLogs];
                END;
                """);
        }
    }
}
