// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;

namespace ArcaneVault.Models.Results;

public enum RegistrationStatus
{
    Success,
    DuplicateUserName,
    DuplicateEmail,
    UserRoleUnavailable,
    Conflict
}

public record RegistrationResult(
    RegistrationStatus Status,
    RegisterResponse? Response = null);
