using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace barberShop.Migrations
{
    /// <inheritdoc />
    public partial class MunkaidoSzunet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FodraszMunkaidok",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FodraszId = table.Column<int>(type: "int", nullable: false),
                    Datum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kezdoido = table.Column<TimeSpan>(type: "time", nullable: false),
                    ZaroIdo = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FodraszMunkaidok", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FodraszMunkaidok_Fodraszok_FodraszId",
                        column: x => x.FodraszId,
                        principalTable: "Fodraszok",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FodraszSzunetek",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FodraszId = table.Column<int>(type: "int", nullable: false),
                    Datum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KezdoIdo = table.Column<TimeSpan>(type: "time", nullable: false),
                    ZaroIdo = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FodraszSzunetek", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FodraszSzunetek_Fodraszok_FodraszId",
                        column: x => x.FodraszId,
                        principalTable: "Fodraszok",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FodraszMunkaidok_FodraszId_Datum",
                table: "FodraszMunkaidok",
                columns: new[] { "FodraszId", "Datum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FodraszSzunetek_FodraszId",
                table: "FodraszSzunetek",
                column: "FodraszId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FodraszMunkaidok");

            migrationBuilder.DropTable(
                name: "FodraszSzunetek");
        }
    }
}
