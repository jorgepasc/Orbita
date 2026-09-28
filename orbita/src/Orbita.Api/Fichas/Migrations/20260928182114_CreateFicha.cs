using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orbita.Api.Fichas.Migrations
{
    /// <inheritdoc />
    public partial class CreateFicha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ficha",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    Terminado = table.Column<bool>(type: "boolean", nullable: false),
                    Valoracion = table.Column<decimal>(type: "numeric", nullable: true),
                    Comentarios = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Compania = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ficha", x => x.Id);
                    table.CheckConstraint("CK_Ficha_Valoracion", "\"Valoracion\" IS NULL OR (\"Valoracion\" >= 0 AND \"Valoracion\" <= 10)");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ficha");
        }
    }
}
