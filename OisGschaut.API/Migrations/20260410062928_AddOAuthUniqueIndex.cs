using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OisGschaut.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_OAuthProvider_OAuthId",
                table: "Users",
                columns: new[] { "OAuthProvider", "OAuthId" },
                unique: true,
                filter: "[OAuthId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_OAuthProvider_OAuthId",
                table: "Users");
        }
    }
}
