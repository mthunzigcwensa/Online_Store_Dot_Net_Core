using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace weSell.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class welldone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_OrderHeaders_OrderHearder",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "OderDate",
                table: "OrderHeaders",
                newName: "OrderDate");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "OrderDetails",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "count",
                table: "OrderDetails",
                newName: "Count");

            migrationBuilder.RenameColumn(
                name: "OrderHearder",
                table: "OrderDetails",
                newName: "OrderHeaderId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetails_OrderHearder",
                table: "OrderDetails",
                newName: "IX_OrderDetails_OrderHeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_OrderHeaders_OrderHeaderId",
                table: "OrderDetails",
                column: "OrderHeaderId",
                principalTable: "OrderHeaders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_OrderHeaders_OrderHeaderId",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "OrderDate",
                table: "OrderHeaders",
                newName: "OderDate");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "OrderDetails",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Count",
                table: "OrderDetails",
                newName: "count");

            migrationBuilder.RenameColumn(
                name: "OrderHeaderId",
                table: "OrderDetails",
                newName: "OrderHearder");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetails_OrderHeaderId",
                table: "OrderDetails",
                newName: "IX_OrderDetails_OrderHearder");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_OrderHeaders_OrderHearder",
                table: "OrderDetails",
                column: "OrderHearder",
                principalTable: "OrderHeaders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
