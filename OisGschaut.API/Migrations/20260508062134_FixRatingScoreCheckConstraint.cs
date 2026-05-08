using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OisGschaut.API.Migrations
{
    /// <inheritdoc />
    public partial class FixRatingScoreCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_ratings_score_range",
                table: "Ratings");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ratings_score_range",
                table: "Ratings",
                sql: "CAST(Score AS REAL) >= 0 AND CAST(Score AS REAL) <= 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_ratings_score_range",
                table: "Ratings");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ratings_score_range",
                table: "Ratings",
                sql: "Score >= 0 AND Score <= 10");
        }
    }
}
