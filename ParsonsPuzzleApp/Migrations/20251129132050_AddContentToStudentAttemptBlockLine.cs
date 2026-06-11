using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddContentToStudentAttemptBlockLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "StudentAttemptBlockLines",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "StudentAttemptBlockLines");
        }
    }
}
