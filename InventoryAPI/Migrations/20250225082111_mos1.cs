using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class mos1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockOutVoucherItems_WarehouseId",
                table: "StockOutVoucherItems",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockOutVoucherItems_Warehouses_WarehouseId",
                table: "StockOutVoucherItems",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockOutVoucherItems_Warehouses_WarehouseId",
                table: "StockOutVoucherItems");

            migrationBuilder.DropIndex(
                name: "IX_StockOutVoucherItems_WarehouseId",
                table: "StockOutVoucherItems");
        }
    }
}
