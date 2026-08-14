using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcaneVault.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeferredTradeConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                table: "Trades",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecipientConfirmedComplete",
                table: "Trades",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequesterConfirmedComplete",
                table: "Trades",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInTrade",
                table: "CollectionItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LockedQuantity",
                table: "CollectionItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Trades accepted by the previous workflow were already transferred.
            migrationBuilder.Sql("UPDATE Trades SET Status = 'Completed' WHERE Status = 'Accepted'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CollectionItems_LockedQuantity_Valid",
                table: "CollectionItems",
                sql: "LockedQuantity >= 0 AND LockedQuantity <= CurrentQuantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CollectionItems_LockedQuantity_Valid",
                table: "CollectionItems");

            migrationBuilder.DropColumn(
                name: "DisputeReason",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "RecipientConfirmedComplete",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "RequesterConfirmedComplete",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "IsInTrade",
                table: "CollectionItems");

            migrationBuilder.DropColumn(
                name: "LockedQuantity",
                table: "CollectionItems");
        }
    }
}
