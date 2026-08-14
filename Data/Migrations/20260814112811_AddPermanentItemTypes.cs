using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcaneVault.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPermanentItemTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemTypeId",
                table: "CollectionItems",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.CreateTable(
                name: "ItemTypes",
                columns: table => new
                {
                    ItemTypeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTypes", x => x.ItemTypeId);
                });

            migrationBuilder.Sql("INSERT INTO ItemTypes (ItemTypeId, ItemName) SELECT 'ITEM-' || ItemId, ItemName FROM CollectionItems");
            migrationBuilder.Sql("UPDATE CollectionItems SET ItemTypeId = 'ITEM-' || ItemId");
            migrationBuilder.Sql("UPDATE CollectionItems SET ItemTypeId = (SELECT source.ItemTypeId FROM TradeItems tradeItem JOIN CollectionItems source ON source.ItemId = tradeItem.CollectionItemId WHERE tradeItem.TransferredCollectionItemId = CollectionItems.ItemId LIMIT 1) WHERE EXISTS (SELECT 1 FROM TradeItems tradeItem WHERE tradeItem.TransferredCollectionItemId = CollectionItems.ItemId)");
            migrationBuilder.Sql("DELETE FROM ItemTypes WHERE ItemTypeId NOT IN (SELECT DISTINCT ItemTypeId FROM CollectionItems)");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionItems_ItemTypeId",
                table: "CollectionItems",
                column: "ItemTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTypes_ItemName",
                table: "ItemTypes",
                column: "ItemName");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionItems_ItemTypes_ItemTypeId",
                table: "CollectionItems",
                column: "ItemTypeId",
                principalTable: "ItemTypes",
                principalColumn: "ItemTypeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionItems_ItemTypes_ItemTypeId",
                table: "CollectionItems");

            migrationBuilder.DropTable(
                name: "ItemTypes");

            migrationBuilder.DropIndex(
                name: "IX_CollectionItems_ItemTypeId",
                table: "CollectionItems");

            migrationBuilder.DropColumn(
                name: "ItemTypeId",
                table: "CollectionItems");
        }
    }
}
