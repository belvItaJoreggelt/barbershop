using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace barberShop.Migrations
{
    /// <inheritdoc />
    public partial class AddKepFajNevVilagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KepFajlNev_Vilagos",
                table: "Szolgaltatasok",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KepFajlNev_Vilagos",
                table: "Szolgaltatasok");
        }
    }
}
