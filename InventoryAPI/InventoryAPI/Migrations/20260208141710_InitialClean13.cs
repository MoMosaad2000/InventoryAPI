using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialClean13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "PaymentReceipts");

            migrationBuilder.RenameColumn(
                name: "ReceiverJobTitle",
                table: "PaymentReceipts",
                newName: "ReceiverJob");

            migrationBuilder.RenameColumn(
                name: "PaymentFor",
                table: "PaymentReceipts",
                newName: "FactoryAddress");

            migrationBuilder.AlterColumn<string>(
                name: "ReceivedFromId",
                table: "PaymentReceipts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptNumber",
                table: "PaymentReceipts",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "CheckNumber",
                table: "PaymentReceipts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AmountInWords",
                table: "PaymentReceipts",
                type: "varchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(300)",
                oldMaxLength: 300)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FactoryCR",
                table: "PaymentReceipts",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FactoryName",
                table: "PaymentReceipts",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FactoryPhone",
                table: "PaymentReceipts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FactoryVAT",
                table: "PaymentReceipts",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "PayCash",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayCheck",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayForDownPayment",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayForFinal",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayForOther",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayForQuotation",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayForReady",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PayForRef",
                table: "PaymentReceipts",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "PayForSalesOrder",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayForStage",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayMada",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayTransfer",
                table: "PaymentReceipts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FactoryCR",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "FactoryName",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "FactoryPhone",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "FactoryVAT",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayCash",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayCheck",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayForDownPayment",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayForFinal",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayForOther",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayForQuotation",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayForReady",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayForRef",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayForSalesOrder",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayForStage",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayMada",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PayTransfer",
                table: "PaymentReceipts");

            migrationBuilder.RenameColumn(
                name: "ReceiverJob",
                table: "PaymentReceipts",
                newName: "ReceiverJobTitle");

            migrationBuilder.RenameColumn(
                name: "FactoryAddress",
                table: "PaymentReceipts",
                newName: "PaymentFor");

            migrationBuilder.AlterColumn<string>(
                name: "ReceivedFromId",
                table: "PaymentReceipts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptNumber",
                table: "PaymentReceipts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "CheckNumber",
                table: "PaymentReceipts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AmountInWords",
                table: "PaymentReceipts",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(400)",
                oldMaxLength: 400)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "PaymentReceipts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
