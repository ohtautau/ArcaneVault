// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.Data;

public static class DbInitializer
{
    private const int StaffRoleId = 2;
    private const string StaffRoleName = "Staff";
    private const int UserRoleId = 1;
    private const string UserRoleName = "User";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("ArcaneVaultDatabase");
        await ReconcilePartiallyAppliedAnalyticsMigrationAsync(connectionString);
        var dbContext = services.GetRequiredService<ArcaneVaultDbContext>();
        await dbContext.Database.MigrateAsync();
        await RepairMissingAnalyticsColumnsAsync(connectionString);

        var passwordHasher = services
            .GetRequiredService<IPasswordHasher<ArcaneVaultUser>>();
        await SeedStaffAsync(dbContext, configuration, passwordHasher);
        await SeedDemoDataAsync(dbContext, configuration, passwordHasher);
    }

    private static async Task ReconcilePartiallyAppliedAnalyticsMigrationAsync(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        if (!await TableExistsAsync(connection, "__EFMigrationsHistory")
            || !await TableExistsAsync(connection, "ArcaneVaultUsers")
            || !await TableExistsAsync(connection, "CollectionItems")) return;

        const string migrationId = "20260811000000_AddAnalyticsTimestamps";
        await using var historyCommand = connection.CreateCommand();
        historyCommand.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = $migrationId";
        historyCommand.Parameters.AddWithValue("$migrationId", migrationId);
        if (Convert.ToInt32(await historyCommand.ExecuteScalarAsync()) > 0) return;

        var userColumnExists = await ColumnExistsAsync(connection, "ArcaneVaultUsers", "CreatedAtUtc");
        var itemColumnExists = await ColumnExistsAsync(connection, "CollectionItems", "CreatedAtUtc");
        if (!userColumnExists && !itemColumnExists) return;

        await EnsureTimestampColumnAsync(connection, "ArcaneVaultUsers");
        await EnsureTimestampColumnAsync(connection, "CollectionItems");
        await using var markCommand = connection.CreateCommand();
        markCommand.CommandText = "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ($migrationId, $productVersion)";
        markCommand.Parameters.AddWithValue("$migrationId", migrationId);
        markCommand.Parameters.AddWithValue("$productVersion", "10.0.0");
        await markCommand.ExecuteNonQueryAsync();
    }

    private static async Task SeedStaffAsync(
        ArcaneVaultDbContext dbContext,
        IConfiguration configuration,
        IPasswordHasher<ArcaneVaultUser> passwordHasher)
    {
        var userName = configuration["SeedStaff:UserName"]?.Trim();
        var email = configuration["SeedStaff:Email"]?.Trim();
        var password = configuration["SeedStaff:Password"];

        if (string.IsNullOrWhiteSpace(userName)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var staffRoleExists = await dbContext.ArcaneVaultUserRoles
            .AnyAsync(role =>
                role.RoleId == StaffRoleId && role.RoleName == StaffRoleName);

        if (!staffRoleExists)
        {
            throw new InvalidOperationException("The Staff role has not been configured.");
        }

        var accountExists = await dbContext.ArcaneVaultUsers
            .IgnoreQueryFilters()
            .AnyAsync(user => user.UserName == userName || user.Email == email);

        if (accountExists)
        {
            return;
        }

        var staffUser = new ArcaneVaultUser
        {
            UserName = userName,
            Email = email,
            IsDeleted = false,
            RoleId = StaffRoleId
        };
        staffUser.PasswordHash = passwordHasher.HashPassword(staffUser, password);

        dbContext.ArcaneVaultUsers.Add(staffUser);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedDemoDataAsync(
        ArcaneVaultDbContext dbContext,
        IConfiguration configuration,
        IPasswordHasher<ArcaneVaultUser> passwordHasher)
    {
        var section = configuration.GetSection("SeedDemoData");
        if (!section.GetValue<bool>("Enabled")) return;

        var defaultPassword = section["DefaultPassword"];
        var users = section.GetSection("Users").Get<List<DemoUser>>() ?? [];
        var categories = section.GetSection("Categories").Get<List<DemoCategory>>() ?? [];
        var items = section.GetSection("Items").Get<List<DemoItem>>() ?? [];
        if (string.IsNullOrWhiteSpace(defaultPassword))
            throw new InvalidOperationException("SeedDemoData:DefaultPassword is required when demo data is enabled.");

        var userRoleExists = await dbContext.ArcaneVaultUserRoles.AnyAsync(role =>
            role.RoleId == UserRoleId && role.RoleName == UserRoleName);
        if (!userRoleExists) throw new InvalidOperationException("The User role has not been configured.");

        foreach (var definition in users)
        {
            var userName = definition.UserName.Trim();
            var email = definition.Email.Trim();
            var exists = await dbContext.ArcaneVaultUsers.IgnoreQueryFilters()
                .AnyAsync(user => user.UserName == userName || user.Email == email);
            if (exists) continue;

            var user = new ArcaneVaultUser
            {
                UserName = userName,
                Email = email,
                RoleId = UserRoleId
            };
            user.PasswordHash = passwordHasher.HashPassword(user, defaultPassword);
            dbContext.ArcaneVaultUsers.Add(user);
        }

        foreach (var definition in categories)
        {
            var code = definition.CategoryCode.Trim().ToUpperInvariant();
            if (!await dbContext.Categories.AnyAsync(category => category.CategoryCode == code))
                dbContext.Categories.Add(new Category { CategoryCode = code, CategoryName = definition.CategoryName.Trim() });
        }

        foreach (var definition in items)
        {
            var typeId = definition.ItemTypeId.Trim().ToUpperInvariant();
            if (!await dbContext.ItemTypes.AnyAsync(type => type.ItemTypeId == typeId))
                dbContext.ItemTypes.Add(new ItemType { ItemTypeId = typeId, ItemName = definition.ItemName.Trim() });
        }
        await dbContext.SaveChangesAsync();

        foreach (var definition in items)
        {
            var owner = definition.OwnerUserName.Trim();
            var typeId = definition.ItemTypeId.Trim().ToUpperInvariant();
            var exists = await dbContext.CollectionItems.IgnoreQueryFilters().AnyAsync(item =>
                item.UserName == owner && item.ItemTypeId == typeId
                && item.Condition == definition.Condition && item.Rarity == definition.Rarity);
            if (exists) continue;

            var item = new CollectionItem
            {
                UserName = owner,
                ItemTypeId = typeId,
                ItemName = definition.ItemName.Trim(),
                Condition = definition.Condition,
                Rarity = definition.Rarity,
                StartingQuantity = definition.StartingQuantity,
                CurrentQuantity = definition.StartingQuantity,
                CollectionItemCategories = definition.CategoryCodes
                    .Select(code => new CollectionItemCategory { CategoryCode = code.Trim().ToUpperInvariant() })
                    .ToList()
            };
            item.QuantityHistory.Add(new CollectionItemQuantityHistory
            {
                Quantity = item.CurrentQuantity,
                ChangedAtUtc = DateTime.UtcNow
            });
            dbContext.CollectionItems.Add(item);
        }
        await dbContext.SaveChangesAsync();
    }

    private sealed class DemoUser
    {
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }

    private sealed class DemoCategory
    {
        public string CategoryCode { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
    }

    private sealed class DemoItem
    {
        public string OwnerUserName { get; init; } = string.Empty;
        public string ItemTypeId { get; init; } = string.Empty;
        public string ItemName { get; init; } = string.Empty;
        public string Condition { get; init; } = "Good";
        public string Rarity { get; init; } = "Common";
        public int StartingQuantity { get; init; }
        public List<string> CategoryCodes { get; init; } = [];
    }

    private static async Task RepairMissingAnalyticsColumnsAsync(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await EnsureTimestampColumnAsync(connection, "ArcaneVaultUsers");
        await EnsureTimestampColumnAsync(connection, "CollectionItems");
    }

    private static async Task EnsureTimestampColumnAsync(SqliteConnection connection, string tableName)
    {
        if (!await TableExistsAsync(connection, tableName)) return;

        if (await ColumnExistsAsync(connection, tableName, "CreatedAtUtc")) return;

        await using var repairCommand = connection.CreateCommand();
        repairCommand.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"CreatedAtUtc\" TEXT NOT NULL DEFAULT '2026-08-11 00:00:00'";
        await repairCommand.ExecuteNonQueryAsync();
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName";
        command.Parameters.AddWithValue("$tableName", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(
        SqliteConnection connection, string tableName, string columnName)
    {
        await using var columnsCommand = connection.CreateCommand();
        columnsCommand.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await columnsCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
