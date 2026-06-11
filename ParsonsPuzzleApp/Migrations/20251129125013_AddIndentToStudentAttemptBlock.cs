using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIndentToStudentAttemptBlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDistractor",
                table: "MiniBlocks",
                newName: "IsCorrect");

            migrationBuilder.AddColumn<int>(
                name: "Indent",
                table: "StudentAttemptBlocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Indent",
                table: "StudentAttemptBlocks");

            migrationBuilder.RenameColumn(
                name: "IsCorrect",
                table: "MiniBlocks",
                newName: "IsDistractor");
        }
    }
}
