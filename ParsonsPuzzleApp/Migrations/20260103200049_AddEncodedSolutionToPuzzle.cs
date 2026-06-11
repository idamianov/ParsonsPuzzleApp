using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEncodedSolutionToPuzzle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncodedSolution",
                table: "Puzzles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncodedSolution",
                table: "Puzzles");
        }
    }
}
