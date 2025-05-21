using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class LOD12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Drawing",
                table: "SalesOrderItem",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "SalesOrderItem",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Drawing",
                table: "SalesOrderItem");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "SalesOrderItem");
        }
    }
}
