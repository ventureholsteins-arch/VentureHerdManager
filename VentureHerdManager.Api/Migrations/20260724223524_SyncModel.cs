using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentureHerdManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOfficial",
                table: "ClassificationRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOfficial",
                table: "ClassificationRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
