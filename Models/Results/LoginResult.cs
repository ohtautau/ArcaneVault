// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;

namespace ArcaneVault.Models.Results;

public enum LoginStatus
{
    Success,
    InvalidCredentials
}

public record LoginResult(
    LoginStatus Status,
    LoginResponse? Response = null);
