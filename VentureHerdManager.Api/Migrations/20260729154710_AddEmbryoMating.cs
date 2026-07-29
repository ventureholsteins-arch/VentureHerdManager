using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations;

public partial class AddEmbryoMating : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Mating",
            table: "EmbryoRecords",
            type: "nvarchar(400)",
            maxLength: 400,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Mating",
            table: "EmbryoRecords");
    }
}
