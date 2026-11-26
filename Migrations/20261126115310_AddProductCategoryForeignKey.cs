using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UltraPokemon.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoryForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_ElementType",
                table: "Products",
                column: "ElementType");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_ElementType",
                table: "Products",
                column: "ElementType",
                principalTable: "Categories",
                principalColumn: "Name",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_ElementType",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ElementType",
                table: "Products");
        }
    }
}
