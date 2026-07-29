using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOfficialToClassificationRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ClassificationRecords_AgeInMonths",
                table: "ClassificationRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ClassificationRecords_Score",
                table: "ClassificationRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CalvingEvents_NumberOfCalves",
                table: "CalvingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppearanceSettings_BackgroundOpacity",
                table: "AppearanceSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppearanceSettings_OverlayOpacity",
                table: "AppearanceSettings");

            migrationBuilder.DropColumn(
                name: "BuyerName",
                table: "Animals");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedNextHeatStart",
                table: "HeatEvents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedNextHeatEnd",
                table: "HeatEvents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbryoImplantDate",
                table: "HeatEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasEmbryoTransfer",
                table: "HeatEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ClassificationRecords",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ClassificationDate",
                table: "ClassificationRecords",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "AgeInMonthsAtScoring",
                table: "ClassificationRecords",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsOfficial",
                table: "ClassificationRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RecommendedDryOffDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PregnancyCheckDueDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedDueDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CloseUpDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateIndex(
                name: "IX_HeatEvents_EmbryoImplantDate",
                table: "HeatEvents",
                column: "EmbryoImplantDate");

            migrationBuilder.CreateIndex(
                name: "IX_HeatEvents_HasEmbryoTransfer",
                table: "HeatEvents",
                column: "HasEmbryoTransfer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HeatEvents_EmbryoImplantDate",
                table: "HeatEvents");

            migrationBuilder.DropIndex(
                name: "IX_HeatEvents_HasEmbryoTransfer",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "EmbryoImplantDate",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "HasEmbryoTransfer",
                table: "HeatEvents");

            migrationBuilder.DropColumn(
                name: "IsOfficial",
                table: "ClassificationRecords");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedNextHeatStart",
                table: "HeatEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedNextHeatEnd",
                table: "HeatEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ClassificationRecords",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ClassificationDate",
                table: "ClassificationRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AgeInMonthsAtScoring",
                table: "ClassificationRecords",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RecommendedDryOffDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PregnancyCheckDueDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedDueDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CloseUpDate",
                table: "BreedingEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerName",
                table: "Animals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClassificationRecords_AgeInMonths",
                table: "ClassificationRecords",
                sql: "[AgeInMonthsAtScoring] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClassificationRecords_Score",
                table: "ClassificationRecords",
                sql: "[Score] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CalvingEvents_NumberOfCalves",
                table: "CalvingEvents",
                sql: "[NumberOfCalves] >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppearanceSettings_BackgroundOpacity",
                table: "AppearanceSettings",
                sql: "[BackgroundOpacity] >= 0 AND [BackgroundOpacity] <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppearanceSettings_OverlayOpacity",
                table: "AppearanceSettings",
                sql: "[OverlayOpacity] >= 0 AND [OverlayOpacity] <= 1");
        }
    }
}
