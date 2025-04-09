using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class mosa123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperatingOrder",
                table: "StockOutVouchers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColorCode",
                table: "StockOutVoucherItems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OperatingOrder",
                table: "StockInVouchers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColorCode",
                table: "StockInVoucherItems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperatingOrder",
                table: "StockOutVouchers");

            migrationBuilder.DropColumn(
                name: "ColorCode",
                table: "StockOutVoucherItems");

            migrationBuilder.DropColumn(
                name: "OperatingOrder",
                table: "StockInVouchers");

            migrationBuilder.DropColumn(
                name: "ColorCode",
                table: "StockInVoucherItems");
        }
    }
}
