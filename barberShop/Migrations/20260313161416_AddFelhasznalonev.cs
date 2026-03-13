using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace barberShop.Migrations
{
    /// <inheritdoc />
    public partial class AddFelhasznalonev : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nev",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nev",
                table: "AspNetUsers");
        }
    }
}
