using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalNotes",
                columns: table => new
                {
                    AnimalNoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    NoteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoteText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalNotes", x => x.AnimalNoteId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimalNotes");
        }
    }
}
