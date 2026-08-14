// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Controllers;

[ApiController, Authorize, Route("api/wishlist")]
public class WishlistController(IWishlistService wishlistService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WishlistItemResponse>>> GetAll([FromQuery] string? search, [FromQuery] bool othersOnly, CancellationToken cancellationToken) =>
        Ok(await wishlistService.GetAllAsync(search, othersOnly ? CurrentUserName : null, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WishlistItemResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Map(await wishlistService.GetByIdAsync(id, cancellationToken), id);

    [HttpPost]
    public async Task<ActionResult<WishlistItemResponse>> Create(CreateWishlistItemRequest request, CancellationToken cancellationToken)
    {
        var result = await wishlistService.CreateAsync(CurrentUserName, request, cancellationToken);
        return result.Status == WishlistStatus.Success && result.Response is not null
            ? CreatedAtAction(nameof(GetById), new { id = result.Response.WishlistItemId }, result.Response)
            : Problem(statusCode: 409, title: "Wishlist conflict.", detail: "The wishlist item could not be created.");
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WishlistItemResponse>> Update(int id, UpdateWishlistItemRequest request, CancellationToken cancellationToken) =>
        Map(await wishlistService.UpdateAsync(id, CurrentUserName, request, cancellationToken), id);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await wishlistService.DeleteAsync(id, CurrentUserName, cancellationToken);
        return result.Status switch
        {
            WishlistStatus.Success => NoContent(), WishlistStatus.Forbidden => Forbid(),
            WishlistStatus.NotFound => NotFoundProblem(id),
            WishlistStatus.HasPendingTrades => Problem(statusCode: 409, title: "Pending trades exist.", detail: "Resolve or cancel pending trade requests before deleting this wishlist item."),
            _ => Problem(statusCode: 409, title: "Wishlist conflict.", detail: "The wishlist item could not be deleted.")
        };
    }

    private string CurrentUserName => User.Identity?.Name ?? throw new InvalidOperationException("Authenticated username is unavailable.");
    private ActionResult<WishlistItemResponse> Map(WishlistResult result, int id) => result.Status switch
    {
        WishlistStatus.Success when result.Response is not null => Ok(result.Response), WishlistStatus.Forbidden => Forbid(),
        WishlistStatus.NotFound => NotFoundProblem(id), _ => Problem(statusCode: 409, title: "Wishlist conflict.")
    };
    private ObjectResult NotFoundProblem(int id) => Problem(statusCode: 404, title: "Wishlist item not found.", detail: $"No wishlist item with ID '{id}' was found.");
}
