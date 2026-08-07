// Name:
// Student Admin No.:
// Tutorial Group:

using System.Security.Claims;
using ArcaneVault.Models.Responses;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ArcaneVault.Services;

public static class AuthenticationPrincipalFactory
{
    public static ClaimsPrincipal Create(LoginResponse response)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, response.UserName),
            new(ClaimTypes.Name, response.UserName),
            new(ClaimTypes.Role, response.RoleName)
        };
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }
}
