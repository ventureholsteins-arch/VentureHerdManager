using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNaabSireCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SireReferences",
                columns: table => new
                {
                    SireReferenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BreedCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ControllerNumber = table.Column<int>(type: "int", nullable: true),
                    StudCode = table.Column<int>(type: "int", nullable: true),
                    NaabBreedCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BullNumber = table.Column<int>(type: "int", nullable: true),
                    NaabCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RegistryStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MarketingStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    YieldReliability = table.Column<int>(type: "int", nullable: true),
                    PtaMilk = table.Column<int>(type: "int", nullable: true),
                    PtaFat = table.Column<int>(type: "int", nullable: true),
                    PtaFatPercent = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    PtaProtein = table.Column<int>(type: "int", nullable: true),
                    PtaProteinPercent = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    SomaticCellScore = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    ProductiveLife = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    DaughterPregnancyRate = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    HeiferConceptionRate = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    CowConceptionRate = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    Livability = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    NetMerit = table.Column<int>(type: "int", nullable: true),
                    SireCalvingEase = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    DaughterCalvingEase = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    PtaType = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    TotalPerformanceIndex = table.Column<int>(type: "int", nullable: true),
                    UdderComposite = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    FeetLegsComposite = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    SourceRowHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SireReferences", x => x.SireReferenceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SireReferences_ImportKey",
                table: "SireReferences",
                column: "ImportKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SireReferences_NaabCode",
                table: "SireReferences",
                column: "NaabCode");

            migrationBuilder.CreateIndex(
                name: "IX_SireReferences_Name",
                table: "SireReferences",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SireReferences_RegistrationNumber",
                table: "SireReferences",
                column: "RegistrationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SireReferences_ShortName",
                table: "SireReferences",
                column: "ShortName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SireReferences");
        }
    }
}
