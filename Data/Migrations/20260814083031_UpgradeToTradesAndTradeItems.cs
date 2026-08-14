using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcaneVault.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeToTradesAndTradeItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeRequests");

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    TradeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequesterUserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    RecipientUserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    WishlistItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StaffResolutionNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ResolvedByStaffUserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.TradeId);
                    table.ForeignKey(
                        name: "FK_Trades_WishlistItems_WishlistItemId",
                        column: x => x.WishlistItemId,
                        principalTable: "WishlistItems",
                        principalColumn: "WishlistItemId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TradeItems",
                columns: table => new
                {
                    TradeItemId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TradeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Side = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeItems", x => x.TradeItemId);
                    table.ForeignKey(
                        name: "FK_TradeItems_CollectionItems_CollectionItemId",
                        column: x => x.CollectionItemId,
                        principalTable: "CollectionItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeItems_Trades_TradeId",
                        column: x => x.TradeId,
                        principalTable: "Trades",
                        principalColumn: "TradeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeItems_CollectionItemId",
                table: "TradeItems",
                column: "CollectionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeItems_TradeId_CollectionItemId",
                table: "TradeItems",
                columns: new[] { "TradeId", "CollectionItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trades_RecipientUserName_Status",
                table: "Trades",
                columns: new[] { "RecipientUserName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_RequesterUserName_Status",
                table: "Trades",
                columns: new[] { "RequesterUserName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_WishlistItemId",
                table: "Trades",
                column: "WishlistItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeItems");

            migrationBuilder.DropTable(
                name: "Trades");

            migrationBuilder.CreateTable(
                name: "TradeRequests",
                columns: table => new
                {
                    TradeRequestId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OfferedCollectionItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    WishlistItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OwnerUserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    RequesterUserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    RespondedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeRequests", x => x.TradeRequestId);
                    table.ForeignKey(
                        name: "FK_TradeRequests_CollectionItems_OfferedCollectionItemId",
                        column: x => x.OfferedCollectionItemId,
                        principalTable: "CollectionItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeRequests_WishlistItems_WishlistItemId",
                        column: x => x.WishlistItemId,
                        principalTable: "WishlistItems",
                        principalColumn: "WishlistItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeRequests_OfferedCollectionItemId",
                table: "TradeRequests",
                column: "OfferedCollectionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeRequests_OwnerUserName_Status",
                table: "TradeRequests",
                columns: new[] { "OwnerUserName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TradeRequests_RequesterUserName_Status",
                table: "TradeRequests",
                columns: new[] { "RequesterUserName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TradeRequests_WishlistItemId",
                table: "TradeRequests",
                column: "WishlistItemId");
        }
    }
}
