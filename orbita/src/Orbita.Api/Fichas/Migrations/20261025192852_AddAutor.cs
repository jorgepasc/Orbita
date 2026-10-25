using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orbita.Api.Fichas.Migrations
{
    /// <inheritdoc />
    public partial class AddAutor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Autor",
                table: "Ficha",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Autor",
                table: "Ficha");
        }
    }
}
