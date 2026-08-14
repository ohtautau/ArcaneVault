using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcaneVault.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeDuplicateItemTypeInventories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE TEMP TABLE InventoryMergeMap AS SELECT duplicate.ItemId AS OldId, (SELECT MIN(candidate.ItemId) FROM CollectionItems candidate WHERE candidate.UserName = duplicate.UserName AND candidate.ItemTypeId = duplicate.ItemTypeId AND candidate.IsDeleted = 0) AS KeepId FROM CollectionItems duplicate WHERE duplicate.IsDeleted = 0 AND duplicate.ItemId <> (SELECT MIN(candidate.ItemId) FROM CollectionItems candidate WHERE candidate.UserName = duplicate.UserName AND candidate.ItemTypeId = duplicate.ItemTypeId AND candidate.IsDeleted = 0)");
            migrationBuilder.Sql("UPDATE CollectionItems SET StartingQuantity = (SELECT SUM(source.StartingQuantity) FROM CollectionItems source WHERE source.UserName = CollectionItems.UserName AND source.ItemTypeId = CollectionItems.ItemTypeId AND source.IsDeleted = 0), CurrentQuantity = (SELECT SUM(source.CurrentQuantity) FROM CollectionItems source WHERE source.UserName = CollectionItems.UserName AND source.ItemTypeId = CollectionItems.ItemTypeId AND source.IsDeleted = 0), LockedQuantity = (SELECT SUM(source.LockedQuantity) FROM CollectionItems source WHERE source.UserName = CollectionItems.UserName AND source.ItemTypeId = CollectionItems.ItemTypeId AND source.IsDeleted = 0), IsInTrade = (SELECT MAX(source.IsInTrade) FROM CollectionItems source WHERE source.UserName = CollectionItems.UserName AND source.ItemTypeId = CollectionItems.ItemTypeId AND source.IsDeleted = 0) WHERE ItemId IN (SELECT KeepId FROM InventoryMergeMap)");
            migrationBuilder.Sql("UPDATE TradeItems SET CollectionItemId = (SELECT KeepId FROM InventoryMergeMap WHERE OldId = TradeItems.CollectionItemId) WHERE CollectionItemId IN (SELECT OldId FROM InventoryMergeMap) AND NOT EXISTS (SELECT 1 FROM TradeItems existing JOIN InventoryMergeMap map ON map.OldId = TradeItems.CollectionItemId WHERE existing.TradeId = TradeItems.TradeId AND existing.CollectionItemId = map.KeepId)");
            migrationBuilder.Sql("UPDATE TradeItems SET Quantity = Quantity + COALESCE((SELECT SUM(duplicate.Quantity) FROM TradeItems duplicate JOIN InventoryMergeMap map ON map.OldId = duplicate.CollectionItemId WHERE duplicate.TradeId = TradeItems.TradeId AND map.KeepId = TradeItems.CollectionItemId), 0) WHERE CollectionItemId IN (SELECT KeepId FROM InventoryMergeMap)");
            migrationBuilder.Sql("DELETE FROM TradeItems WHERE CollectionItemId IN (SELECT OldId FROM InventoryMergeMap)");
            migrationBuilder.Sql("UPDATE TradeItems SET TransferredCollectionItemId = (SELECT KeepId FROM InventoryMergeMap WHERE OldId = TradeItems.TransferredCollectionItemId) WHERE TransferredCollectionItemId IN (SELECT OldId FROM InventoryMergeMap)");
            migrationBuilder.Sql("UPDATE CollectionItemQuantityHistory SET ItemId = (SELECT KeepId FROM InventoryMergeMap WHERE OldId = CollectionItemQuantityHistory.ItemId) WHERE ItemId IN (SELECT OldId FROM InventoryMergeMap)");
            migrationBuilder.Sql("INSERT OR IGNORE INTO CollectionItemCategories (ItemId, CategoryCode) SELECT map.KeepId, category.CategoryCode FROM CollectionItemCategories category JOIN InventoryMergeMap map ON map.OldId = category.ItemId");
            migrationBuilder.Sql("DELETE FROM CollectionItems WHERE ItemId IN (SELECT OldId FROM InventoryMergeMap)");
            migrationBuilder.Sql("DROP TABLE InventoryMergeMap");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Consolidated inventory records cannot be split without inventing ownership history.
        }
    }
}
