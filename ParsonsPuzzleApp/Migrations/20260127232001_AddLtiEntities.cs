using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLtiEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LtiPlatforms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Issuer = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorizationEndpoint = table.Column<string>(type: "TEXT", nullable: false),
                    TokenEndpoint = table.Column<string>(type: "TEXT", nullable: false),
                    JwksUrl = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InstructorId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiPlatforms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LtiPlatforms_AspNetUsers_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LtiStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Nonce = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    TargetLinkUri = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiDeployments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeploymentId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    BundleId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LtiPlatformId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LtiDeployments_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LtiDeployments_LtiPlatforms_LtiPlatformId",
                        column: x => x.LtiPlatformId,
                        principalTable: "LtiPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LtiDeployments_BundleId",
                table: "LtiDeployments",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_LtiDeployments_LtiPlatformId_DeploymentId",
                table: "LtiDeployments",
                columns: new[] { "LtiPlatformId", "DeploymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiPlatforms_InstructorId",
                table: "LtiPlatforms",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_LtiPlatforms_Issuer_ClientId",
                table: "LtiPlatforms",
                columns: new[] { "Issuer", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiStates_ExpiresAt",
                table: "LtiStates",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_LtiStates_State",
                table: "LtiStates",
                column: "State",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LtiDeployments");

            migrationBuilder.DropTable(
                name: "LtiStates");

            migrationBuilder.DropTable(
                name: "LtiPlatforms");
        }
    }
}
