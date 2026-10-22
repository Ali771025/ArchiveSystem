using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    public partial class AddIndependentOwnersAndDossierOwnerJoin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Owners",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HomePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IdentityNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NationalId = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Owners_CompanyName",
                table: "Owners",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_Owners_FirstName",
                table: "Owners",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_Owners_LastName",
                table: "Owners",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Owners_NationalCode",
                table: "Owners",
                column: "NationalCode",
                unique: true,
                filter: "[NationalCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Owners_NationalId",
                table: "Owners",
                column: "NationalId",
                unique: true,
                filter: "[NationalId] IS NOT NULL");

            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "DossierOwners",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [Owners] (
                    [OwnerType],
                    [Mobile],
                    [PostalCode],
                    [Address],
                    [HomePhone],
                    [FirstName],
                    [LastName],
                    [NationalCode],
                    [IdentityNumber],
                    [CompanyName],
                    [NationalId],
                    [RegistrationNumber],
                    [CreatedAt],
                    [ModifiedAt]
                )
                SELECT
                    CASE WHEN [source].[PersonType] = 2 THEN 2 ELSE 1 END AS [OwnerType],
                    [source].[MobileNumber] AS [Mobile],
                    NULL AS [PostalCode],
                    NULL AS [Address],
                    [source].[PhoneNumber] AS [HomePhone],
                    CASE WHEN [source].[PersonType] = 2 THEN NULL ELSE [source].[FirstName] END AS [FirstName],
                    CASE WHEN [source].[PersonType] = 2 THEN NULL ELSE [source].[LastName] END AS [LastName],
                    CASE WHEN [source].[PersonType] = 2 THEN NULL ELSE LEFT([source].[NationalId], 10) END AS [NationalCode],
                    NULL AS [IdentityNumber],
                    CASE WHEN [source].[PersonType] = 2 THEN CONCAT([source].[FirstName], N' ', [source].[LastName]) ELSE NULL END AS [CompanyName],
                    CASE WHEN [source].[PersonType] = 2 THEN LEFT([source].[NationalId], 11) ELSE NULL END AS [NationalId],
                    NULL AS [RegistrationNumber],
                    SYSUTCDATETIME(),
                    NULL
                FROM (
                    SELECT
                        [PersonType],
                        [NationalId],
                        MIN([Id]) AS [FirstOwnerRowId]
                    FROM [DossierOwners]
                    GROUP BY [PersonType], [NationalId]
                ) AS [dedup]
                INNER JOIN [DossierOwners] AS [source] ON [source].[Id] = [dedup].[FirstOwnerRowId]
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Owners] AS [existing]
                    WHERE (
                        [source].[PersonType] = 2
                        AND [existing].[OwnerType] = 2
                        AND COALESCE([existing].[NationalId], N'') = COALESCE(LEFT([source].[NationalId], 11), N'')
                    )
                    OR (
                        [source].[PersonType] <> 2
                        AND [existing].[OwnerType] = 1
                        AND COALESCE([existing].[NationalCode], N'') = COALESCE(LEFT([source].[NationalId], 10), N'')
                    )
                );

                UPDATE [links]
                SET [links].[OwnerId] = [owners].[Id]
                FROM [DossierOwners] AS [links]
                INNER JOIN [Owners] AS [owners]
                    ON (
                        ([owners].[OwnerType] = 2 AND [links].[PersonType] = 2 AND [owners].[NationalId] = LEFT([links].[NationalId], 11))
                        OR
                        ([owners].[OwnerType] = 1 AND [links].[PersonType] <> 2 AND [owners].[NationalCode] = LEFT([links].[NationalId], 10))
                    );
                """);

            migrationBuilder.Sql(
                """
                WITH [active_links] AS
                (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER (PARTITION BY [DossierId], [OwnerId] ORDER BY [Id]) AS [rn]
                    FROM [DossierOwners]
                    WHERE [DeletedAt] IS NULL
                )
                UPDATE [dup]
                SET
                    [dup].[DeletedAt] = COALESCE([dup].[DeletedAt], SYSUTCDATETIME()),
                    [dup].[ModifiedAt] = SYSUTCDATETIME()
                FROM [DossierOwners] AS [dup]
                INNER JOIN [active_links] AS [x] ON [x].[Id] = [dup].[Id]
                WHERE [x].[rn] > 1;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "OwnerId",
                table: "DossierOwners",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_DossierOwners_DossierId_NationalId",
                table: "DossierOwners");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "DossierOwners");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "DossierOwners");

            migrationBuilder.DropColumn(
                name: "MobileNumber",
                table: "DossierOwners");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "DossierOwners");

            migrationBuilder.DropColumn(
                name: "PersonType",
                table: "DossierOwners");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "DossierOwners");

            migrationBuilder.CreateIndex(
                name: "IX_DossierOwners_OwnerId",
                table: "DossierOwners",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DossierOwners_DossierId_OwnerId_Active",
                table: "DossierOwners",
                columns: new[] { "DossierId", "OwnerId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_DossierOwners_Owners_OwnerId",
                table: "DossierOwners",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DossierOwners_Owners_OwnerId",
                table: "DossierOwners");

            migrationBuilder.DropIndex(
                name: "IX_DossierOwners_DossierId_OwnerId_Active",
                table: "DossierOwners");

            migrationBuilder.DropIndex(
                name: "IX_DossierOwners_OwnerId",
                table: "DossierOwners");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "DossierOwners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "DossierOwners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "MobileNumber",
                table: "DossierOwners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "DossierOwners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "PersonType",
                table: "DossierOwners",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "DossierOwners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [links]
                SET
                    [links].[FirstName] = CASE WHEN [owners].[OwnerType] = 2 THEN [owners].[CompanyName] ELSE COALESCE([owners].[FirstName], N'') END,
                    [links].[LastName] = CASE WHEN [owners].[OwnerType] = 2 THEN N'' ELSE COALESCE([owners].[LastName], N'') END,
                    [links].[NationalId] = COALESCE([owners].[NationalCode], [owners].[NationalId], N''),
                    [links].[PersonType] = CASE WHEN [owners].[OwnerType] = 2 THEN 2 ELSE 1 END,
                    [links].[MobileNumber] = [owners].[Mobile],
                    [links].[PhoneNumber] = [owners].[HomePhone]
                FROM [DossierOwners] AS [links]
                INNER JOIN [Owners] AS [owners] ON [owners].[Id] = [links].[OwnerId];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DossierOwners_DossierId_NationalId",
                table: "DossierOwners",
                columns: new[] { "DossierId", "NationalId" });

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "DossierOwners");

            migrationBuilder.DropTable(
                name: "Owners");
        }
    }
}
