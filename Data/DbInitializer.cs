// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.Data;

public static class DbInitializer
{
    private const int StaffRoleId = 2;
    private const string StaffRoleName = "Staff";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<ArcaneVaultDbContext>();
        await dbContext.Database.MigrateAsync();

        var configuration = services.GetRequiredService<IConfiguration>();
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
}
