using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcaneVault.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionItemConditionRarityAndPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "CollectionItems",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "Good");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "CollectionItems",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rarity",
                table: "CollectionItems",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Common");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CollectionItems_Condition_Valid",
                table: "CollectionItems",
                sql: "Condition IN ('Mint', 'Good', 'Fair', 'Poor')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CollectionItems_Rarity_Valid",
                table: "CollectionItems",
                sql: "Rarity IN ('Common', 'Rare', 'Ultra Rare')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CollectionItems_Condition_Valid",
                table: "CollectionItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CollectionItems_Rarity_Valid",
                table: "CollectionItems");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "CollectionItems");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "CollectionItems");

            migrationBuilder.DropColumn(
                name: "Rarity",
                table: "CollectionItems");
        }
    }
}
