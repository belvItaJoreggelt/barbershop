using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace barberShop.Migrations
{
    /// <inheritdoc />
    public partial class UniqueEsedessegiIdopont : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Idopontok_FodraszId",
                table: "Idopontok");

            migrationBuilder.CreateIndex(
                name: "IX_Idopontok_FodraszId_EsedekessegiIdopont",
                table: "Idopontok",
                columns: new[] { "FodraszId", "EsedekessegiIdopont" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Idopontok_FodraszId_EsedekessegiIdopont",
                table: "Idopontok");

            migrationBuilder.CreateIndex(
                name: "IX_Idopontok_FodraszId",
                table: "Idopontok",
                column: "FodraszId");
        }
    }
}
