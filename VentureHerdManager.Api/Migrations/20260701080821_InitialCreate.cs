using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Animals",
                columns: table => new
                {
                    AnimalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalStatus = table.Column<int>(type: "int", nullable: false),
                    BarnName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegisteredName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Sex = table.Column<int>(type: "int", nullable: false),
                    AnimalStage = table.Column<int>(type: "int", nullable: false),
                    Breed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SireId = table.Column<int>(type: "int", nullable: true),
                    SireName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DamId = table.Column<int>(type: "int", nullable: true),
                    DamName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animals", x => x.AnimalId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Animals");
        }
    }
}
