using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddedLanguageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Language",
                table: "Puzzles",
                newName: "LanguageId");

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    CommentSyntax = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CodeMirrorMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            // Seed languages first before creating foreign key constraint
            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "Id", "Name", "DisplayName", "Category", "CommentSyntax", "CodeMirrorMode", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "C", "C", 1, "//", "clike", true, 1 },
                    { 2, "Cpp", "C++", 1, "//", "clike", true, 2 },
                    { 3, "CSharp", "C#", 1, "//", "clike", true, 3 },
                    { 4, "Java", "Java", 1, "//", "clike", true, 4 },
                    { 5, "JavaScript", "JavaScript", 1, "//", "javascript", true, 5 },
                    { 6, "Python", "Python", 2, "#", "python", true, 6 },
                    { 7, "TSQL", "T-SQL", 3, "--", "sql", true, 7 },
                    { 8, "MySQL", "MySQL", 3, "--", "sql", true, 8 },
                    { 9, "PostgreSQL", "PostgreSQL", 3, "--", "sql", true, 9 },
                    { 10, "plSQL", "PL/SQL", 3, "--", "sql", true, 10 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Puzzles_LanguageId",
                table: "Puzzles",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Name",
                table: "Languages",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Puzzles_Languages_LanguageId",
                table: "Puzzles",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Puzzles_Languages_LanguageId",
                table: "Puzzles");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropIndex(
                name: "IX_Puzzles_LanguageId",
                table: "Puzzles");

            migrationBuilder.RenameColumn(
                name: "LanguageId",
                table: "Puzzles",
                newName: "Language");
        }
    }
}
