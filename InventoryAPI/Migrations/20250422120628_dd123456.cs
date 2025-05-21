using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class dd123456 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "SalesOrderItem");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "SalesOrderItem",
                newName: "Unit");

            migrationBuilder.AddColumn<string>(
                name: "OrderName",
                table: "SalesOrderItem",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderName",
                table: "SalesOrderItem");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "SalesOrderItem",
                newName: "ProductName");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "SalesOrderItem",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
