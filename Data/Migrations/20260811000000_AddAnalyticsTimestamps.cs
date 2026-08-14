// Name:
// Student Admin No.:
// Tutorial Group:

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ArcaneVault.Data.Migrations;

[DbContext(typeof(ArcaneVaultDbContext))]
[Migration("20260811000000_AddAnalyticsTimestamps")]
public partial class AddAnalyticsTimestamps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var migrationTime = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);

        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAtUtc",
            table: "CollectionItems",
            type: "TEXT",
            nullable: false,
            defaultValue: migrationTime);

        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAtUtc",
            table: "ArcaneVaultUsers",
            type: "TEXT",
            nullable: false,
            defaultValue: migrationTime);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CreatedAtUtc", table: "CollectionItems");
        migrationBuilder.DropColumn(name: "CreatedAtUtc", table: "ArcaneVaultUsers");
    }
}
