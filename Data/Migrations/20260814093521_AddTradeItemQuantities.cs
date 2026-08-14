using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcaneVault.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeItemQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "TradeItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TransferredCollectionItemId",
                table: "TradeItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradeItems_TransferredCollectionItemId",
                table: "TradeItems",
                column: "TransferredCollectionItemId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TradeItems_Quantity_Positive",
                table: "TradeItems",
                sql: "Quantity > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_TradeItems_CollectionItems_TransferredCollectionItemId",
                table: "TradeItems",
                column: "TransferredCollectionItemId",
                principalTable: "CollectionItems",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradeItems_CollectionItems_TransferredCollectionItemId",
                table: "TradeItems");

            migrationBuilder.DropIndex(
                name: "IX_TradeItems_TransferredCollectionItemId",
                table: "TradeItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TradeItems_Quantity_Positive",
                table: "TradeItems");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "TradeItems");

            migrationBuilder.DropColumn(
                name: "TransferredCollectionItemId",
                table: "TradeItems");
        }
    }
}
