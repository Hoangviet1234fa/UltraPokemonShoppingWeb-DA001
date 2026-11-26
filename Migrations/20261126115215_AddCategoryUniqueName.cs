using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UltraPokemon.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryUniqueName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Categories_Name",
                table: "Categories",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Categories_Name",
                table: "Categories");
        }
    }
}
