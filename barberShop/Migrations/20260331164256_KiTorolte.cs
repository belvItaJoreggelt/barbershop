using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace barberShop.Migrations
{
    /// <inheritdoc />
    public partial class KiTorolte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KiTorolte",
                table: "ToroltIdopontok",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KiTorolte",
                table: "ToroltIdopontok");
        }
    }
}
