// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Data;
using ArcaneVault.Models.Entities;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.Services;

public class AccountService(
    ArcaneVaultDbContext dbContext,
    IPasswordHasher<ArcaneVaultUser> passwordHasher) : IAccountService
{
    private const int UserRoleId = 1;
    private const string UserRoleName = "User";

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim();
        var user = await dbContext.ArcaneVaultUsers
            .Include(account => account.Role)
            .SingleOrDefaultAsync(
                account => account.UserName == userName,
                cancellationToken);

        if (user is null)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var response = new LoginResponse
        {
            UserName = user.UserName,
            RoleName = user.Role.RoleName,
            Message = "Login successful."
        };

        return new LoginResult(LoginStatus.Success, response);
    }

    public async Task<RegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        if (await UserNameExistsAsync(userName, cancellationToken))
        {
            return new RegistrationResult(RegistrationStatus.DuplicateUserName);
        }

        if (await EmailExistsAsync(email, cancellationToken))
        {
            return new RegistrationResult(RegistrationStatus.DuplicateEmail);
        }

        var userRoleExists = await dbContext.ArcaneVaultUserRoles
            .AnyAsync(
                role => role.RoleId == UserRoleId && role.RoleName == UserRoleName,
                cancellationToken);

        if (!userRoleExists)
        {
            return new RegistrationResult(RegistrationStatus.UserRoleUnavailable);
        }

        var user = new ArcaneVaultUser
        {
            UserName = userName,
            Email = email,
            IsDeleted = false,
            RoleId = UserRoleId
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.ArcaneVaultUsers.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            dbContext.Entry(user).State = EntityState.Detached;

            if (await UserNameExistsAsync(userName, cancellationToken))
            {
                return new RegistrationResult(RegistrationStatus.DuplicateUserName);
            }

            if (await EmailExistsAsync(email, cancellationToken))
            {
                return new RegistrationResult(RegistrationStatus.DuplicateEmail);
            }

            return new RegistrationResult(RegistrationStatus.Conflict);
        }

        var response = new RegisterResponse
        {
            UserName = user.UserName,
            Email = user.Email,
            RoleName = UserRoleName,
            Message = "Registration successful."
        };

        return new RegistrationResult(RegistrationStatus.Success, response);
    }

    private Task<bool> UserNameExistsAsync(
        string userName,
        CancellationToken cancellationToken) =>
        dbContext.ArcaneVaultUsers
            .IgnoreQueryFilters()
            .AnyAsync(user => user.UserName == userName, cancellationToken);

    private Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken) =>
        dbContext.ArcaneVaultUsers
            .IgnoreQueryFilters()
            .AnyAsync(user => user.Email == email, cancellationToken);
}
