using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcaneVault.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistTrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WishlistItems",
                columns: table => new
                {
                    WishlistItemId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DesiredQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistItems", x => x.WishlistItemId);
                    table.CheckConstraint("CK_WishlistItems_DesiredQuantity_Positive", "DesiredQuantity > 0");
                    table.ForeignKey(
                        name: "FK_WishlistItems_ArcaneVaultUsers_UserName",
                        column: x => x.UserName,
                        principalTable: "ArcaneVaultUsers",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeRequests",
                columns: table => new
                {
                    TradeRequestId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WishlistItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferedCollectionItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequesterUserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    OwnerUserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_UserName",
                table: "WishlistItems",
                column: "UserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeRequests");

            migrationBuilder.DropTable(
                name: "WishlistItems");
        }
    }
}
