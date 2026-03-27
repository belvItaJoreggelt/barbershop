using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace barberShop.Migrations
{
    /// <inheritdoc />
    public partial class ToroltIdoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToroltIdopontok",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FodraszId = table.Column<int>(type: "integer", nullable: false),
                    SzolgaltatasId = table.Column<int>(type: "integer", nullable: false),
                    EsedekessegiIdopont = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FoglalasiIdopont = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerNeve = table.Column<string>(type: "text", nullable: false),
                    CustomerEmail = table.Column<string>(type: "text", nullable: false),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    CustomerNotes = table.Column<string>(type: "text", nullable: true),
                    TorolveUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToroltIdopontok", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ToroltIdopontok_Fodraszok_FodraszId",
                        column: x => x.FodraszId,
                        principalTable: "Fodraszok",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToroltIdopontok_Szolgaltatasok_SzolgaltatasId",
                        column: x => x.SzolgaltatasId,
                        principalTable: "Szolgaltatasok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToroltIdopontok_FodraszId",
                table: "ToroltIdopontok",
                column: "FodraszId");

            migrationBuilder.CreateIndex(
                name: "IX_ToroltIdopontok_SzolgaltatasId",
                table: "ToroltIdopontok",
                column: "SzolgaltatasId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToroltIdopontok");
        }
    }
}
