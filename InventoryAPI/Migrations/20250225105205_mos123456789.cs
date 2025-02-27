using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class mos123456789 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "StockOutVoucherItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockOutVoucherItems_CustomerId",
                table: "StockOutVoucherItems",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockOutVoucherItems_Customers_CustomerId",
                table: "StockOutVoucherItems",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockOutVoucherItems_Customers_CustomerId",
                table: "StockOutVoucherItems");

            migrationBuilder.DropIndex(
                name: "IX_StockOutVoucherItems_CustomerId",
                table: "StockOutVoucherItems");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "StockOutVoucherItems");
        }
    }
}
