using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcaneVault.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionItemQuantityHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionItemQuantityHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionItemQuantityHistory", x => x.HistoryId);
                    table.CheckConstraint("CK_CollectionItemQuantityHistory_Quantity_NonNegative", "Quantity >= 0");
                    table.ForeignKey(
                        name: "FK_CollectionItemQuantityHistory_CollectionItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "CollectionItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionItemQuantityHistory_ItemId_ChangedAtUtc",
                table: "CollectionItemQuantityHistory",
                columns: new[] { "ItemId", "ChangedAtUtc" });

            // Give existing inventory a baseline point so its chart is useful immediately.
            migrationBuilder.Sql("""
                INSERT INTO CollectionItemQuantityHistory (ItemId, Quantity, ChangedAtUtc)
                SELECT ItemId, CurrentQuantity, CURRENT_TIMESTAMP
                FROM CollectionItems
                WHERE IsDeleted = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionItemQuantityHistory");
        }
    }
}
