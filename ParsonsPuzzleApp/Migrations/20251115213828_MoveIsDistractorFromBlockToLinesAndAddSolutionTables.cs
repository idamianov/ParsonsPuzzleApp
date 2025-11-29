using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class MoveIsDistractorFromBlockToLinesAndAddSolutionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDistractor",
                table: "PuzzleBlocks");

            migrationBuilder.DropColumn(
                name: "SlotName",
                table: "PuzzleBlocks");

            migrationBuilder.RenameColumn(
                name: "IsCorrect",
                table: "MiniBlocks",
                newName: "IsDistractor");

            migrationBuilder.AddColumn<bool>(
                name: "IsDistractor",
                table: "PuzzleBlockLines",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StudentAttemptBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentAttemptId = table.Column<int>(type: "INTEGER", nullable: false),
                    PuzzleBlockId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAttemptBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAttemptBlocks_PuzzleBlocks_PuzzleBlockId",
                        column: x => x.PuzzleBlockId,
                        principalTable: "PuzzleBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAttemptBlocks_StudentAttempts_StudentAttemptId",
                        column: x => x.StudentAttemptId,
                        principalTable: "StudentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAttemptBlockLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentAttemptBlockId = table.Column<int>(type: "INTEGER", nullable: false),
                    PuzzleBlockLineId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAttemptBlockLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAttemptBlockLines_PuzzleBlockLines_PuzzleBlockLineId",
                        column: x => x.PuzzleBlockLineId,
                        principalTable: "PuzzleBlockLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAttemptBlockLines_StudentAttemptBlocks_StudentAttemptBlockId",
                        column: x => x.StudentAttemptBlockId,
                        principalTable: "StudentAttemptBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAttemptMiniBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentAttemptBlockLineId = table.Column<int>(type: "INTEGER", nullable: true),
                    MiniBlockId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAttemptMiniBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAttemptMiniBlocks_MiniBlocks_MiniBlockId",
                        column: x => x.MiniBlockId,
                        principalTable: "MiniBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAttemptMiniBlocks_StudentAttemptBlockLines_StudentAttemptBlockLineId",
                        column: x => x.StudentAttemptBlockLineId,
                        principalTable: "StudentAttemptBlockLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttemptBlockLines_PuzzleBlockLineId",
                table: "StudentAttemptBlockLines",
                column: "PuzzleBlockLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttemptBlockLines_StudentAttemptBlockId",
                table: "StudentAttemptBlockLines",
                column: "StudentAttemptBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttemptBlocks_PuzzleBlockId",
                table: "StudentAttemptBlocks",
                column: "PuzzleBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttemptBlocks_StudentAttemptId",
                table: "StudentAttemptBlocks",
                column: "StudentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttemptMiniBlocks_MiniBlockId",
                table: "StudentAttemptMiniBlocks",
                column: "MiniBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttemptMiniBlocks_StudentAttemptBlockLineId",
                table: "StudentAttemptMiniBlocks",
                column: "StudentAttemptBlockLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentAttemptMiniBlocks");

            migrationBuilder.DropTable(
                name: "StudentAttemptBlockLines");

            migrationBuilder.DropTable(
                name: "StudentAttemptBlocks");

            migrationBuilder.DropColumn(
                name: "IsDistractor",
                table: "PuzzleBlockLines");

            migrationBuilder.RenameColumn(
                name: "IsDistractor",
                table: "MiniBlocks",
                newName: "IsCorrect");

            migrationBuilder.AddColumn<bool>(
                name: "IsDistractor",
                table: "PuzzleBlocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SlotName",
                table: "PuzzleBlocks",
                type: "TEXT",
                nullable: true);
        }
    }
}
