using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEnglishDeclarativeLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT OR IGNORE INTO \"Languages\" (\"Id\", \"Name\", \"DisplayName\", \"Category\", \"CommentSyntax\", \"CodeMirrorMode\", \"IsActive\", \"SortOrder\") " +
                "VALUES (11, 'English', 'English', 4, '//', 'text/plain', 1, 11);");

            migrationBuilder.DropForeignKey(
                name: "FK_LtiSessions_Bundles_BundleId",
                table: "LtiSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_LtiSessions_LtiPlatforms_LtiPlatformId",
                table: "LtiSessions");

            migrationBuilder.CreateIndex(
                name: "IX_LtiAccessTokens_ExpiresAt",
                table: "LtiAccessTokens",
                column: "ExpiresAt");

            migrationBuilder.AddForeignKey(
                name: "FK_LtiSessions_Bundles_BundleId",
                table: "LtiSessions",
                column: "BundleId",
                principalTable: "Bundles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LtiSessions_LtiPlatforms_LtiPlatformId",
                table: "LtiSessions",
                column: "LtiPlatformId",
                principalTable: "LtiPlatforms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DropForeignKey(
                name: "FK_LtiSessions_Bundles_BundleId",
                table: "LtiSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_LtiSessions_LtiPlatforms_LtiPlatformId",
                table: "LtiSessions");

            migrationBuilder.DropIndex(
                name: "IX_LtiAccessTokens_ExpiresAt",
                table: "LtiAccessTokens");

            migrationBuilder.AddForeignKey(
                name: "FK_LtiSessions_Bundles_BundleId",
                table: "LtiSessions",
                column: "BundleId",
                principalTable: "Bundles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LtiSessions_LtiPlatforms_LtiPlatformId",
                table: "LtiSessions",
                column: "LtiPlatformId",
                principalTable: "LtiPlatforms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
