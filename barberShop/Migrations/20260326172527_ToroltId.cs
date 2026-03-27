using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace barberShop.Migrations
{
    /// <inheritdoc />
    public partial class ToroltId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ID",
                table: "ToroltIdopontok",
                newName: "Id");

            migrationBuilder.AddColumn<int>(
                name: "EredetiIdopontId",
                table: "ToroltIdopontok",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EredetiIdopontId",
                table: "ToroltIdopontok");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ToroltIdopontok",
                newName: "ID");
        }
    }
}
