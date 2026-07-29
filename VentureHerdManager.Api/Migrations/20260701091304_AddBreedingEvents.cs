using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBreedingEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BreedingEvents",
                columns: table => new
                {
                    BreedingEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    BreedingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SireUsed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BreedingType = table.Column<int>(type: "int", nullable: false),
                    ExpectedDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PregnancyCheckDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PregnancyStatus = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreedingEvents", x => x.BreedingEventId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BreedingEvents");
        }
    }
}
