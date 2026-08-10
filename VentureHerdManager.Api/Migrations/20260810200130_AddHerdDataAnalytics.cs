using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHerdDataAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalIdentityMappings",
                columns: table => new
                {
                    AnimalIdentityMappingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<int>(type: "int", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SourceLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalIdentityMappings", x => x.AnimalIdentityMappingId);
                    table.ForeignKey(
                        name: "FK_AnimalIdentityMappings_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "AnimalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HerdDataImports",
                columns: table => new
                {
                    HerdDataImportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RowsImported = table.Column<int>(type: "int", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HerdDataImports", x => x.HerdDataImportId);
                });

            migrationBuilder.CreateTable(
                name: "AnimalDataRecords",
                columns: table => new
                {
                    AnimalDataRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HerdDataImportId = table.Column<int>(type: "int", nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceAnimalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceAnimalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OfficialId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DaysInMilk = table.Column<int>(type: "int", nullable: true),
                    Milk = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    FatPercent = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    ProteinPercent = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    LastCalvingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Tpi = table.Column<int>(type: "int", nullable: true),
                    NetMerit = table.Column<int>(type: "int", nullable: true),
                    MilkPta = table.Column<int>(type: "int", nullable: true),
                    FatPta = table.Column<int>(type: "int", nullable: true),
                    ProteinPta = table.Column<int>(type: "int", nullable: true),
                    SomaticCellScore = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    DaughterPregnancyRate = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    ProductiveLife = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    TypeScore = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    UdderComposite = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    FeetLegsComposite = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    RawDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalDataRecords", x => x.AnimalDataRecordId);
                    table.ForeignKey(
                        name: "FK_AnimalDataRecords_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "AnimalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnimalDataRecords_HerdDataImports_HerdDataImportId",
                        column: x => x.HerdDataImportId,
                        principalTable: "HerdDataImports",
                        principalColumn: "HerdDataImportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalDataRecords_AnimalId_Source_ReportDate",
                table: "AnimalDataRecords",
                columns: new[] { "AnimalId", "Source", "ReportDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalDataRecords_HerdDataImportId",
                table: "AnimalDataRecords",
                column: "HerdDataImportId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalIdentityMappings_AnimalId",
                table: "AnimalIdentityMappings",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalIdentityMappings_Source_SourceKey",
                table: "AnimalIdentityMappings",
                columns: new[] { "Source", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HerdDataImports_FileHash",
                table: "HerdDataImports",
                column: "FileHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HerdDataImports_Source_ReportDate",
                table: "HerdDataImports",
                columns: new[] { "Source", "ReportDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimalDataRecords");

            migrationBuilder.DropTable(
                name: "AnimalIdentityMappings");

            migrationBuilder.DropTable(
                name: "HerdDataImports");
        }
    }
}
