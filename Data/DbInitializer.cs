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

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        await RepairMissingAnalyticsColumnsAsync(
            configuration.GetConnectionString("ArcaneVaultDatabase"));
        var dbContext = services.GetRequiredService<ArcaneVaultDbContext>();
        await dbContext.Database.MigrateAsync();

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
        var passwordHasher = services
            .GetRequiredService<IPasswordHasher<ArcaneVaultUser>>();
        staffUser.PasswordHash = passwordHasher.HashPassword(staffUser, password);

        dbContext.ArcaneVaultUsers.Add(staffUser);
        await dbContext.SaveChangesAsync();
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
        await using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName";
        tableCommand.Parameters.AddWithValue("$tableName", tableName);
        if (Convert.ToInt32(await tableCommand.ExecuteScalarAsync()) == 0) return;

        await using var columnsCommand = connection.CreateCommand();
        columnsCommand.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await columnsCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), "CreatedAtUtc", StringComparison.OrdinalIgnoreCase)) return;
        }
        await reader.DisposeAsync();

        await using var repairCommand = connection.CreateCommand();
        repairCommand.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"CreatedAtUtc\" TEXT NOT NULL DEFAULT '2026-08-11 00:00:00'";
        await repairCommand.ExecuteNonQueryAsync();
    }
}
