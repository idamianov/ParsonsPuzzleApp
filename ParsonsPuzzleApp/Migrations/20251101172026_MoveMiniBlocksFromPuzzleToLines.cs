using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class MoveMiniBlocksFromPuzzleToLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM MiniBlocks;");

            migrationBuilder.DropForeignKey(
                name: "FK_MiniBlocks_Puzzles_PuzzleId",
                table: "MiniBlocks");

            migrationBuilder.DropColumn(
                name: "BlockConfiguration",
                table: "Puzzles");

            migrationBuilder.RenameColumn(
                name: "PuzzleId",
                table: "MiniBlocks",
                newName: "PuzzleBlockLineId");

            migrationBuilder.RenameIndex(
                name: "IX_MiniBlocks_PuzzleId",
                table: "MiniBlocks",
                newName: "IX_MiniBlocks_PuzzleBlockLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_MiniBlocks_PuzzleBlockLines_PuzzleBlockLineId",
                table: "MiniBlocks",
                column: "PuzzleBlockLineId",
                principalTable: "PuzzleBlockLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MiniBlocks_PuzzleBlockLines_PuzzleBlockLineId",
                table: "MiniBlocks");

            migrationBuilder.RenameColumn(
                name: "PuzzleBlockLineId",
                table: "MiniBlocks",
                newName: "PuzzleId");

            migrationBuilder.RenameIndex(
                name: "IX_MiniBlocks_PuzzleBlockLineId",
                table: "MiniBlocks",
                newName: "IX_MiniBlocks_PuzzleId");

            migrationBuilder.AddColumn<string>(
                name: "BlockConfiguration",
                table: "Puzzles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MiniBlocks_Puzzles_PuzzleId",
                table: "MiniBlocks",
                column: "PuzzleId",
                principalTable: "Puzzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
