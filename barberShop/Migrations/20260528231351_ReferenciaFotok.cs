using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace barberShop.Migrations
{
    /// <inheritdoc />
    public partial class ReferenciaFotok : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FodraszReferenciaFotok",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FodraszId = table.Column<int>(type: "integer", nullable: false),
                    Fajlnev = table.Column<string>(type: "text", nullable: false),
                    FeltoltveUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Sorrend = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FodraszReferenciaFotok", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FodraszReferenciaFotok_Fodraszok_FodraszId",
                        column: x => x.FodraszId,
                        principalTable: "Fodraszok",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FodraszReferenciaFotok_FodraszId",
                table: "FodraszReferenciaFotok",
                column: "FodraszId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FodraszReferenciaFotok");
        }
    }
}
