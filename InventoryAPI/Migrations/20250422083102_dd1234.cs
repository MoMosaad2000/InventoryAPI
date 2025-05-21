using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class dd1234 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderItem_Products_ProductId",
                table: "SalesOrderItem");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderItem_ProductId",
                table: "SalesOrderItem");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "SalesOrderItem",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "SalesOrderItem");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItem_ProductId",
                table: "SalesOrderItem",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderItem_Products_ProductId",
                table: "SalesOrderItem",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
