using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLtiGradePassback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LtiAccessTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LtiPlatformId = table.Column<int>(type: "INTEGER", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiAccessTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LtiAccessTokens_LtiPlatforms_LtiPlatformId",
                        column: x => x.LtiPlatformId,
                        principalTable: "LtiPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LtiResourceLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LtiPlatformId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeploymentId = table.Column<string>(type: "TEXT", nullable: false),
                    ResourceLinkId = table.Column<string>(type: "TEXT", nullable: false),
                    LineItemUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LineItemsUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ScopesRaw = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiResourceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LtiResourceLinks_LtiPlatforms_LtiPlatformId",
                        column: x => x.LtiPlatformId,
                        principalTable: "LtiPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LtiSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    LtiPlatformId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeploymentId = table.Column<string>(type: "TEXT", nullable: false),
                    ResourceLinkId = table.Column<string>(type: "TEXT", nullable: true),
                    BundleAttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BundleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ContextTitle = table.Column<string>(type: "TEXT", nullable: true),
                    LaunchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GradeSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    GradeSentAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LtiSessions_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LtiSessions_LtiPlatforms_LtiPlatformId",
                        column: x => x.LtiPlatformId,
                        principalTable: "LtiPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LtiAccessTokens_LtiPlatformId",
                table: "LtiAccessTokens",
                column: "LtiPlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_LtiResourceLinks_LtiPlatformId_ResourceLinkId",
                table: "LtiResourceLinks",
                columns: new[] { "LtiPlatformId", "ResourceLinkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiSessions_BundleId",
                table: "LtiSessions",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_LtiSessions_LtiPlatformId",
                table: "LtiSessions",
                column: "LtiPlatformId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LtiAccessTokens");

            migrationBuilder.DropTable(
                name: "LtiResourceLinks");

            migrationBuilder.DropTable(
                name: "LtiSessions");
        }
    }
}
