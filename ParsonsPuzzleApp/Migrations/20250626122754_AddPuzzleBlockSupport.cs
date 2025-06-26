using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPuzzleBlockSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockConfiguration",
                table: "Puzzles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PuzzleBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PuzzleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDistractor = table.Column<bool>(type: "INTEGER", nullable: false),
                    SlotName = table.Column<string>(type: "TEXT", nullable: false),
                    IsMultiline = table.Column<bool>(type: "INTEGER", nullable: false),
                    GroupId = table.Column<string>(type: "TEXT", nullable: false),
                    IsOrderIndependent = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuzzleBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuzzleBlocks_Puzzles_PuzzleId",
                        column: x => x.PuzzleId,
                        principalTable: "Puzzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PuzzleBlockLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PuzzleBlockId = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    LineOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuzzleBlockLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuzzleBlockLines_PuzzleBlocks_PuzzleBlockId",
                        column: x => x.PuzzleBlockId,
                        principalTable: "PuzzleBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuzzleBlockLines_PuzzleBlockId",
                table: "PuzzleBlockLines",
                column: "PuzzleBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_PuzzleBlocks_PuzzleId",
                table: "PuzzleBlocks",
                column: "PuzzleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PuzzleBlockLines");

            migrationBuilder.DropTable(
                name: "PuzzleBlocks");

            migrationBuilder.DropColumn(
                name: "BlockConfiguration",
                table: "Puzzles");
        }
    }
}
