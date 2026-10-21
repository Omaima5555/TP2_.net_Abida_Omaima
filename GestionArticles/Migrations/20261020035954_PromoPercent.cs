using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionArticles.Migrations
{
    /// <inheritdoc />
    public partial class PromoPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PromoPercent",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromoPercent",
                table: "Products");
        }
    }
}
