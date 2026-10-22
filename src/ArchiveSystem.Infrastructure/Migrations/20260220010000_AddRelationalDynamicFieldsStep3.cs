using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    public partial class AddRelationalDynamicFieldsStep3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DynamicTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassificationId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DynamicTemplates_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DynamicFieldGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicFieldGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DynamicFieldGroups_DynamicTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "DynamicTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DynamicFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    MinNumber = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MaxNumber = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MinLength = table.Column<int>(type: "int", nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    Regex = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsMultiSelect = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DynamicFieldDefinitions_DynamicFieldGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "DynamicFieldGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DynamicFieldOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicFieldOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DynamicFieldOptions_DynamicFieldDefinitions_FieldId",
                        column: x => x.FieldId,
                        principalTable: "DynamicFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseFieldValueBools",
                columns: table => new
                {
                    CaseId = table.Column<long>(type: "bigint", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFieldValueBools", x => new { x.CaseId, x.FieldId });
                    table.ForeignKey(
                        name: "FK_CaseFieldValueBools_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseFieldValueBools_DynamicFieldDefinitions_FieldId",
                        column: x => x.FieldId,
                        principalTable: "DynamicFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseFieldValueDates",
                columns: table => new
                {
                    CaseId = table.Column<long>(type: "bigint", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFieldValueDates", x => new { x.CaseId, x.FieldId });
                    table.ForeignKey(
                        name: "FK_CaseFieldValueDates_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseFieldValueDates_DynamicFieldDefinitions_FieldId",
                        column: x => x.FieldId,
                        principalTable: "DynamicFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseFieldValueNumbers",
                columns: table => new
                {
                    CaseId = table.Column<long>(type: "bigint", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFieldValueNumbers", x => new { x.CaseId, x.FieldId });
                    table.ForeignKey(
                        name: "FK_CaseFieldValueNumbers_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseFieldValueNumbers_DynamicFieldDefinitions_FieldId",
                        column: x => x.FieldId,
                        principalTable: "DynamicFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseFieldValueStrings",
                columns: table => new
                {
                    CaseId = table.Column<long>(type: "bigint", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFieldValueStrings", x => new { x.CaseId, x.FieldId });
                    table.ForeignKey(
                        name: "FK_CaseFieldValueStrings_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseFieldValueStrings_DynamicFieldDefinitions_FieldId",
                        column: x => x.FieldId,
                        principalTable: "DynamicFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseFieldValueOptions",
                columns: table => new
                {
                    CaseId = table.Column<long>(type: "bigint", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFieldValueOptions", x => new { x.CaseId, x.FieldId, x.OptionId });
                    table.ForeignKey(
                        name: "FK_CaseFieldValueOptions_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseFieldValueOptions_DynamicFieldDefinitions_FieldId",
                        column: x => x.FieldId,
                        principalTable: "DynamicFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseFieldValueOptions_DynamicFieldOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "DynamicFieldOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueBools_FieldId_Value",
                table: "CaseFieldValueBools",
                columns: new[] { "FieldId", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueBools_FieldId_Value_CaseId",
                table: "CaseFieldValueBools",
                columns: new[] { "FieldId", "Value", "CaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueDates_FieldId_Value",
                table: "CaseFieldValueDates",
                columns: new[] { "FieldId", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueDates_FieldId_Value_CaseId",
                table: "CaseFieldValueDates",
                columns: new[] { "FieldId", "Value", "CaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueNumbers_FieldId_Value",
                table: "CaseFieldValueNumbers",
                columns: new[] { "FieldId", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueNumbers_FieldId_Value_CaseId",
                table: "CaseFieldValueNumbers",
                columns: new[] { "FieldId", "Value", "CaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueOptions_CaseId_FieldId",
                table: "CaseFieldValueOptions",
                columns: new[] { "CaseId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueOptions_FieldId_OptionId",
                table: "CaseFieldValueOptions",
                columns: new[] { "FieldId", "OptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueOptions_OptionId",
                table: "CaseFieldValueOptions",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueStrings_FieldId_Value",
                table: "CaseFieldValueStrings",
                columns: new[] { "FieldId", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldValueStrings_FieldId_Value_CaseId",
                table: "CaseFieldValueStrings",
                columns: new[] { "FieldId", "Value", "CaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_DynamicFieldDefinitions_GroupId_Order",
                table: "DynamicFieldDefinitions",
                columns: new[] { "GroupId", "Order" });

            migrationBuilder.CreateIndex(
                name: "UX_DynamicFieldDefinitions_GroupId_Key",
                table: "DynamicFieldDefinitions",
                columns: new[] { "GroupId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DynamicFieldGroups_TemplateId_Order",
                table: "DynamicFieldGroups",
                columns: new[] { "TemplateId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_DynamicFieldOptions_FieldId_Order",
                table: "DynamicFieldOptions",
                columns: new[] { "FieldId", "Order" });

            migrationBuilder.CreateIndex(
                name: "UX_DynamicFieldOptions_FieldId_Value",
                table: "DynamicFieldOptions",
                columns: new[] { "FieldId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DynamicTemplates_ClassificationId",
                table: "DynamicTemplates",
                column: "ClassificationId",
                unique: true);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Cases', 'DynamicFields') IS NOT NULL
                BEGIN
                    ALTER TABLE [Cases] DROP COLUMN [DynamicFields];
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DynamicFields",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.DropTable(
                name: "CaseFieldValueBools");

            migrationBuilder.DropTable(
                name: "CaseFieldValueDates");

            migrationBuilder.DropTable(
                name: "CaseFieldValueNumbers");

            migrationBuilder.DropTable(
                name: "CaseFieldValueOptions");

            migrationBuilder.DropTable(
                name: "CaseFieldValueStrings");

            migrationBuilder.DropTable(
                name: "DynamicFieldOptions");

            migrationBuilder.DropTable(
                name: "DynamicFieldDefinitions");

            migrationBuilder.DropTable(
                name: "DynamicFieldGroups");

            migrationBuilder.DropTable(
                name: "DynamicTemplates");
        }
    }
}
