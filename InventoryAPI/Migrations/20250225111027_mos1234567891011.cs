using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class mos1234567891011 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockOutVouchers_Warehouses_WarehouseId",
                table: "StockOutVouchers");

            migrationBuilder.DropIndex(
                name: "IX_StockOutVouchers_WarehouseId",
                table: "StockOutVouchers");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "StockOutVouchers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "StockOutVouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockOutVouchers_WarehouseId",
                table: "StockOutVouchers",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockOutVouchers_Warehouses_WarehouseId",
                table: "StockOutVouchers",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
