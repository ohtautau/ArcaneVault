// Name:
// Student Admin No.:
// Tutorial Group:

using System.Security.Claims;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        var userName = User.Identity?.Name;
        var roleName = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(roleName))
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse
        {
            UserName = userName,
            RoleName = roleName
        });
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.LoginAsync(request, cancellationToken);

        if (result.Status != LoginStatus.Success || result.Response is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Login failed.",
                detail: "Invalid username or password.");
        }

        var principal = AuthenticationPrincipalFactory.Create(result.Response);
        var properties = new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            ExpiresUtc = request.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : null
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        return Ok(result.Response);
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.RegisterAsync(request, cancellationToken);

        return result.Status switch
        {
            RegistrationStatus.Success =>
                StatusCode(StatusCodes.Status201Created, result.Response),
            RegistrationStatus.DuplicateUserName =>
                ConflictResponse(
                    "Username already exists.",
                    "Choose a different username."),
            RegistrationStatus.DuplicateEmail =>
                ConflictResponse(
                    "Email already exists.",
                    "Use a different email address."),
            RegistrationStatus.UserRoleUnavailable =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Registration is unavailable.",
                    detail: "The default User role has not been configured."),
            _ => ConflictResponse(
                "Registration conflict.",
                "The account could not be created because the data changed. Try again.")
        };
    }

    private ObjectResult ConflictResponse(string title, string detail) =>
        Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: title,
            detail: detail);
}
