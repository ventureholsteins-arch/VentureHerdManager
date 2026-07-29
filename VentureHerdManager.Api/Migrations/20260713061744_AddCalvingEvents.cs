using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCalvingEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalvingEvents",
                columns: table => new
                {
                    CalvingEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    CalvingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalfSex = table.Column<int>(type: "int", nullable: false),
                    CalfBarnName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalfRegisteredName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalvingEase = table.Column<int>(type: "int", nullable: false),
                    Twins = table.Column<bool>(type: "bit", nullable: false),
                    Stillborn = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalvingEvents", x => x.CalvingEventId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalvingEvents");
        }
    }
}
