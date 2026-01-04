using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class SeedEncodedSolutionForExistingPuzzles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            UPDATE Puzzles 
            SET EncodedSolution = 'a0(bc)0(de)0f0g1h1i1j1k1l1m1n2o2p2(qr)1s2u1v2w1'
            WHERE Id = 3
            ");

            migrationBuilder.Sql(@"
            UPDATE Puzzles 
            SET EncodedSolution = '(ab)0c0(def)0g0(hi)0(jk)0'
            WHERE Id = 4
            ");

            migrationBuilder.Sql(@"
            UPDATE Puzzles 
            SET EncodedSolution = 'a0b0(cd)0(ef)1g2'
            WHERE Id = 5
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            UPDATE Puzzles 
            SET EncodedSolution = ''
            WHERE Id IN (3, 4, 5)
            ");
        }
    }
}
