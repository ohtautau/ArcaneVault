// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Results;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(IAccountService accountService) : ControllerBase
{
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
