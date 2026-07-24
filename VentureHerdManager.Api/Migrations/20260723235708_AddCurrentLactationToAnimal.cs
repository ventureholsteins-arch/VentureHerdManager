using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentLactationToAnimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PictureUrl",
                table: "HeatEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "HeatEvents",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "HeatEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "HeatEvents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedNextHeatEnd",
                table: "HeatEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedNextHeatStart",
                table: "HeatEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "HeatStrength",
                table: "HeatEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "StandingHeat",
                table: "HeatEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "HeatEvents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "HeatEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "DryOffEvents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "DryOffEvents",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "DryOffEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DryOffEvents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DryOffEvents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "DryOffEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "CalvingEvents",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "CalvingEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CalfRegisteredName",
                table: "CalvingEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CalfBarnName",
                table: "CalvingEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BirthWeight",
                table: "CalvingEvents",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalfAnimalId",
                table: "CalvingEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalfRegistrationNumber",
                table: "CalvingEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CalvingEvents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfCalves",
                table: "CalvingEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PictureUrl",
                table: "CalvingEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CalvingEvents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CalvingEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SireUsed",
                table: "BreedingEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "BreedingEvents",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "BreedingEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CloseUpDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "PregnancyCheckDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecommendedDryOffDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Technician",
                table: "BreedingEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "BreedingEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SireName",
                table: "Animals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationNumber",
                table: "Animals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RegisteredName",
                table: "Animals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Animals",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DamName",
                table: "Animals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Breed",
                table: "Animals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BarnName",
                table: "Animals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerName",
                table: "Animals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Animals",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Animals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentLactation",
                table: "Animals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeceasedDate",
                table: "Animals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeceasedNotes",
                table: "Animals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Animals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Animals",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SoldDate",
                table: "Animals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoldNotes",
                table: "Animals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Animals",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Animals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NoteText",
                table: "AnimalNotes",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "AnimalNotes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AnimalNotes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<int>(
                name: "NoteType",
                table: "AnimalNotes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AnimalNotes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateTable(
                name: "AnimalPhotos",
                columns: table => new
                {
                    AnimalPhotoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PhotoType = table.Column<int>(type: "int", nullable: false),
                    PhotoDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RelatedEventId = table.Column<int>(type: "int", nullable: true),
                    RelatedEventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalPhotos", x => x.AnimalPhotoId);
                    table.ForeignKey(
                        name: "FK_AnimalPhotos_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "AnimalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppearanceSettings",
                columns: table => new
                {
                    AppearanceSettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BackgroundImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BackgroundOpacity = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    OverlayOpacity = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccentColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppearanceSettings", x => x.AppearanceSettingId);
                    table.CheckConstraint("CK_AppearanceSettings_BackgroundOpacity", "[BackgroundOpacity] >= 0 AND [BackgroundOpacity] <= 1");
                    table.CheckConstraint("CK_AppearanceSettings_OverlayOpacity", "[OverlayOpacity] >= 0 AND [OverlayOpacity] <= 1");
                });

            migrationBuilder.CreateTable(
                name: "ClassificationRecords",
                columns: table => new
                {
                    ClassificationRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    ClassificationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Baa = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    AgeInMonthsAtScoring = table.Column<int>(type: "int", nullable: false),
                    ClassificationLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationRecords", x => x.ClassificationRecordId);
                    table.CheckConstraint("CK_ClassificationRecords_AgeInMonths", "[AgeInMonthsAtScoring] >= 0");
                    table.CheckConstraint("CK_ClassificationRecords_Score", "[Score] >= 0");
                    table.ForeignKey(
                        name: "FK_ClassificationRecords_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "AnimalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LutalyseEvents",
                columns: table => new
                {
                    LutalyseEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    AdministrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedHeatWatchStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedHeatWatchEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HeatObserved = table.Column<bool>(type: "bit", nullable: false),
                    HeatObservedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultingHeatEventId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LutalyseEvents", x => x.LutalyseEventId);
                    table.ForeignKey(
                        name: "FK_LutalyseEvents_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "AnimalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeatEvents_AnimalId",
                table: "HeatEvents",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_HeatEvents_AnimalId_HeatDateTime",
                table: "HeatEvents",
                columns: new[] { "AnimalId", "HeatDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_HeatEvents_ExpectedNextHeatEnd",
                table: "HeatEvents",
                column: "ExpectedNextHeatEnd");

            migrationBuilder.CreateIndex(
                name: "IX_HeatEvents_ExpectedNextHeatStart",
                table: "HeatEvents",
                column: "ExpectedNextHeatStart");

            migrationBuilder.CreateIndex(
                name: "IX_HeatEvents_HeatDateTime",
                table: "HeatEvents",
                column: "HeatDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_DryOffEvents_AnimalId",
                table: "DryOffEvents",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_DryOffEvents_AnimalId_DryOffDate",
                table: "DryOffEvents",
                columns: new[] { "AnimalId", "DryOffDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DryOffEvents_DryOffDate",
                table: "DryOffEvents",
                column: "DryOffDate");

            migrationBuilder.CreateIndex(
                name: "IX_CalvingEvents_AnimalId",
                table: "CalvingEvents",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_CalvingEvents_AnimalId_CalvingDate",
                table: "CalvingEvents",
                columns: new[] { "AnimalId", "CalvingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CalvingEvents_CalfAnimalId",
                table: "CalvingEvents",
                column: "CalfAnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_CalvingEvents_CalvingDate",
                table: "CalvingEvents",
                column: "CalvingDate");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CalvingEvents_NumberOfCalves",
                table: "CalvingEvents",
                sql: "[NumberOfCalves] >= 1");

            migrationBuilder.CreateIndex(
                name: "IX_BreedingEvents_AnimalId",
                table: "BreedingEvents",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_BreedingEvents_AnimalId_BreedingDate",
                table: "BreedingEvents",
                columns: new[] { "AnimalId", "BreedingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BreedingEvents_BreedingDate",
                table: "BreedingEvents",
                column: "BreedingDate");

            migrationBuilder.CreateIndex(
                name: "IX_BreedingEvents_CloseUpDate",
                table: "BreedingEvents",
                column: "CloseUpDate");

            migrationBuilder.CreateIndex(
                name: "IX_BreedingEvents_ExpectedDueDate",
                table: "BreedingEvents",
                column: "ExpectedDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_BreedingEvents_PregnancyCheckDueDate",
                table: "BreedingEvents",
                column: "PregnancyCheckDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_BreedingEvents_PregnancyStatus",
                table: "BreedingEvents",
                column: "PregnancyStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BreedingEvents_RecommendedDryOffDate",
                table: "BreedingEvents",
                column: "RecommendedDryOffDate");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_AnimalStage",
                table: "Animals",
                column: "AnimalStage");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_AnimalStatus",
                table: "Animals",
                column: "AnimalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_BarnName",
                table: "Animals",
                column: "BarnName");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_DamId",
                table: "Animals",
                column: "DamId");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_IsFavorite",
                table: "Animals",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_RegisteredName",
                table: "Animals",
                column: "RegisteredName");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_RegistrationNumber",
                table: "Animals",
                column: "RegistrationNumber",
                unique: true,
                filter: "[RegistrationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_SireId",
                table: "Animals",
                column: "SireId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalNotes_AnimalId",
                table: "AnimalNotes",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalNotes_AnimalId_NoteDate",
                table: "AnimalNotes",
                columns: new[] { "AnimalId", "NoteDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalNotes_NoteDate",
                table: "AnimalNotes",
                column: "NoteDate");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalPhotos_AnimalId",
                table: "AnimalPhotos",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalPhotos_AnimalId_PhotoDate",
                table: "AnimalPhotos",
                columns: new[] { "AnimalId", "PhotoDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalPhotos_PhotoDate",
                table: "AnimalPhotos",
                column: "PhotoDate");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalPhotos_PhotoType",
                table: "AnimalPhotos",
                column: "PhotoType");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationRecords_AnimalId",
                table: "ClassificationRecords",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationRecords_AnimalId_ClassificationDate",
                table: "ClassificationRecords",
                columns: new[] { "AnimalId", "ClassificationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationRecords_Baa",
                table: "ClassificationRecords",
                column: "Baa");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationRecords_ClassificationDate",
                table: "ClassificationRecords",
                column: "ClassificationDate");

            migrationBuilder.CreateIndex(
                name: "IX_LutalyseEvents_AdministrationDate",
                table: "LutalyseEvents",
                column: "AdministrationDate");

            migrationBuilder.CreateIndex(
                name: "IX_LutalyseEvents_AnimalId",
                table: "LutalyseEvents",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_LutalyseEvents_AnimalId_AdministrationDate",
                table: "LutalyseEvents",
                columns: new[] { "AnimalId", "AdministrationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LutalyseEvents_ExpectedHeatWatchEnd",
                table: "LutalyseEvents",
                column: "ExpectedHeatWatchEnd");

            migrationBuilder.CreateIndex(
                name: "IX_LutalyseEvents_ExpectedHeatWatchStart",
                table: "LutalyseEvents",
                column: "ExpectedHeatWatchStart");

            migrationBuilder.AddForeignKey(
                name: "FK_AnimalNotes_Animals_AnimalId",
                table: "AnimalNotes",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "AnimalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Animals_Animals_DamId",
                table: "Animals",
                column: "DamId",
                principalTable: "Animals",
                principalColumn: "AnimalId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Animals_Animals_SireId",
                table: "Animals",
                column: "SireId",
                principalTable: "Animals",
                principalColumn: "AnimalId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BreedingEvents_Animals_AnimalId",
                table: "BreedingEvents",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "AnimalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalvingEvents_Animals_AnimalId",
                table: "CalvingEvents",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "AnimalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalvingEvents_Animals_CalfAnimalId",
                table: "CalvingEvents",
                column: "CalfAnimalId",
                principalTable: "Animals",
                principalColumn: "AnimalId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DryOffEvents_Animals_AnimalId",
                table: "DryOffEvents",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "AnimalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HeatEvents_Animals_AnimalId",
                table: "HeatEvents",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "AnimalId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnimalNotes_Animals_AnimalId",
                table: "AnimalNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Animals_Animals_DamId",
                table: "Animals");

            migrationBuilder.DropForeignKey(
                name: "FK_Animals_Animals_SireId",
                table: "Animals");

            migrationBuilder.DropForeignKey(
                name: "FK_BreedingEvents_Animals_AnimalId",
                table: "BreedingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_CalvingEvents_Animals_AnimalId",
                table: "CalvingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_CalvingEvents_Animals_CalfAnimalId",
                table: "CalvingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_DryOffEvents_Animals_AnimalId",
                table: "DryOffEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_HeatEvents_Animals_AnimalId",
                table: "HeatEvents");

            migrationBuilder.DropTable(
                name: "AnimalPhotos");

            migrationBuilder.DropTable(
                name: "AppearanceSettings");

            migrationBuilder.DropTable(
                name: "ClassificationRecords");

            migrationBuilder.DropTable(
                name: "LutalyseEvents");

            migrationBuilder.DropIndex(
                name: "IX_HeatEvents_AnimalId",
                table: "HeatEvents");

            migrationBuilder.DropIndex(
                name: "IX_HeatEvents_AnimalId_HeatDateTime",
                table: "HeatEvents");

            migrationBuilder.DropIndex(
                name: "IX_HeatEvents_ExpectedNextHeatEnd",
                table: "HeatEvents");

            migrationBuilder.DropIndex(
                name: "IX_HeatEvents_ExpectedNextHeatStart",
                table: "HeatEvents");

            migrationBuilder.DropIndex(
                name: "IX_HeatEvents_HeatDateTime",
                table: "HeatEvents");

            migrationBuilder.DropIndex(
                name: "IX_DryOffEvents_AnimalId",
                table: "DryOffEvents");

            migrationBuilder.DropIndex(
                name: "IX_DryOffEvents_AnimalId_DryOffDate",
                table: "DryOffEvents");

            migrationBuilder.DropIndex(
                name: "IX_DryOffEvents_DryOffDate",
                table: "DryOffEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalvingEvents_AnimalId",
                table: "CalvingEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalvingEvents_AnimalId_CalvingDate",
                table: "CalvingEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalvingEvents_CalfAnimalId",
                table: "CalvingEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalvingEvents_CalvingDate",
                table: "CalvingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CalvingEvents_NumberOfCalves",
                table: "CalvingEvents");

            migrationBuilder.DropIndex(
                name: "IX_BreedingEvents_AnimalId",
                table: "BreedingEvents");

            migrationBuilder.DropIndex(
                name: "IX_BreedingEvents_AnimalId_BreedingDate",
                table: "BreedingEvents");

            migrationBuilder.DropIndex(
                name: "IX_BreedingEvents_BreedingDate",
                table: "BreedingEvents");

            migrationBuilder.DropIndex(
                name: "IX_BreedingEvents_CloseUpDate",
                table: "BreedingEvents");

            migrationBuilder.DropIndex(
                name: "IX_BreedingEvents_ExpectedDueDate",
                table: "BreedingEvents");

            migrationBuilder.DropIndex(
                name: "IX_BreedingEvents_PregnancyCheckDueDate",
                table: "BreedingEvents");

            migrationBuilder.DropIndex(
                name: "IX_BreedingEvents_PregnancyStatus",
                table: "BreedingEvents");

            migrationBuilder.DropIndex(
                name: "IX_BreedingEvents_RecommendedDryOffDate",
                table: "BreedingEvents");

            migrationBuilder.DropIndex(
                name: "IX_Animals_AnimalStage",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_AnimalStatus",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_BarnName",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_DamId",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_IsFavorite",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_RegisteredName",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_RegistrationNumber",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_SireId",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_AnimalNotes_AnimalId",
                table: "AnimalNotes");

            migrationBuilder.DropIndex(
                name: "IX_AnimalNotes_AnimalId_NoteDate",
                table: "AnimalNotes");

            migrationBuilder.DropIndex(
                name: "IX_AnimalNotes_NoteDate",
                table: "AnimalNotes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "ExpectedNextHeatEnd",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "ExpectedNextHeatStart",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "HeatStrength",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "StandingHeat",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DryOffEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DryOffEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DryOffEvents");

            migrationBuilder.DropColumn(
                name: "BirthWeight",
                table: "CalvingEvents");

            migrationBuilder.DropColumn(
                name: "CalfAnimalId",
                table: "CalvingEvents");

            migrationBuilder.DropColumn(
                name: "CalfRegistrationNumber",
                table: "CalvingEvents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CalvingEvents");

            migrationBuilder.DropColumn(
                name: "NumberOfCalves",
                table: "CalvingEvents");

            migrationBuilder.DropColumn(
                name: "PictureUrl",
                table: "CalvingEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CalvingEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CalvingEvents");

            migrationBuilder.DropColumn(
                name: "CloseUpDate",
                table: "BreedingEvents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BreedingEvents");

            migrationBuilder.DropColumn(
                name: "PregnancyCheckDate",
                table: "BreedingEvents");

            migrationBuilder.DropColumn(
                name: "RecommendedDryOffDate",
                table: "BreedingEvents");

            migrationBuilder.DropColumn(
                name: "Technician",
                table: "BreedingEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BreedingEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "BreedingEvents");

            migrationBuilder.DropColumn(
                name: "BuyerName",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "CurrentLactation",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "DeceasedDate",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "DeceasedNotes",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "SoldDate",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "SoldNotes",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AnimalNotes");

            migrationBuilder.DropColumn(
                name: "NoteType",
                table: "AnimalNotes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AnimalNotes");

            migrationBuilder.AlterColumn<string>(
                name: "PictureUrl",
                table: "HeatEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "HeatEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "HeatEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "DryOffEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "DryOffEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "DryOffEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "CalvingEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "CalvingEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CalfRegisteredName",
                table: "CalvingEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CalfBarnName",
                table: "CalvingEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SireUsed",
                table: "BreedingEvents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "BreedingEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "BreedingEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SireName",
                table: "Animals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationNumber",
                table: "Animals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RegisteredName",
                table: "Animals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Animals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DamName",
                table: "Animals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Breed",
                table: "Animals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BarnName",
                table: "Animals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NoteText",
                table: "AnimalNotes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "AnimalNotes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
