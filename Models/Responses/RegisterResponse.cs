// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Responses;

public class RegisterResponse
{
    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
