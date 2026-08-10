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

[ApiController]
[Authorize]
[Route("api/collection-items")]
public class CollectionItemsController(ICollectionItemService collectionItemService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CollectionItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CollectionItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var items = await collectionItemService.GetAllAsync(
            CurrentUserName,
            User.IsInRole("Staff"),
            cancellationToken);
        return Ok(items);
    }

    [HttpGet("{itemId:int}")]
    [ProducesResponseType<CollectionItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionItemResponse>> GetById(
        int itemId,
        CancellationToken cancellationToken)
    {
        var result = await collectionItemService.GetByIdAsync(
            itemId,
            CurrentUserName,
            User.IsInRole("Staff"),
            cancellationToken);
        return MapItemResult(result, itemId);
    }

    [HttpPost]
    [ProducesResponseType<CollectionItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CollectionItemResponse>> Create(
        CreateCollectionItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await collectionItemService.CreateAsync(
            CurrentUserName,
            request,
            cancellationToken);

        if (result.Status == CollectionItemStatus.Success && result.Response is not null)
        {
            return CreatedAtAction(
                nameof(GetById),
                new { itemId = result.Response.ItemId },
                result.Response);
        }

        return MapWriteFailure(result, 0);
    }

    [HttpPut("{itemId:int}")]
    [ProducesResponseType<CollectionItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CollectionItemResponse>> Update(
        int itemId,
        UpdateCollectionItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await collectionItemService.UpdateAsync(
            itemId,
            CurrentUserName,
            User.IsInRole("Staff"),
            request,
            cancellationToken);

        return result.Status == CollectionItemStatus.Success && result.Response is not null
            ? Ok(result.Response)
            : MapWriteFailure(result, itemId);
    }

    [HttpDelete("{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int itemId,
        CancellationToken cancellationToken)
    {
        var result = await collectionItemService.DeleteAsync(
            itemId,
            CurrentUserName,
            User.IsInRole("Staff"),
            cancellationToken);

        return result.Status switch
        {
            CollectionItemStatus.Success => NoContent(),
            CollectionItemStatus.NotFound => ItemNotFound(itemId),
            CollectionItemStatus.Forbidden => Forbid(),
            _ => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Collection item conflict.",
                detail: "The collection item could not be deleted because the data changed. Try again.")
        };
    }

    private string CurrentUserName =>
        User.Identity?.Name
        ?? throw new InvalidOperationException("The authenticated username is unavailable.");

    private ActionResult<CollectionItemResponse> MapItemResult(
        CollectionItemResult result,
        int itemId) =>
        result.Status switch
        {
            CollectionItemStatus.Success when result.Response is not null => Ok(result.Response),
            CollectionItemStatus.Forbidden => Forbid(),
            _ => ItemNotFound(itemId)
        };

    private ActionResult<CollectionItemResponse> MapWriteFailure(
        CollectionItemResult result,
        int itemId) =>
        result.Status switch
        {
            CollectionItemStatus.NotFound => ItemNotFound(itemId),
            CollectionItemStatus.Forbidden => Forbid(),
            CollectionItemStatus.InvalidCategories => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid categories.",
                detail: $"Unknown category codes: {string.Join(", ", result.InvalidCategoryCodes ?? [])}."),
            _ => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Collection item conflict.",
                detail: "The collection item could not be saved because the data changed. Try again.")
        };

    private ObjectResult ItemNotFound(int itemId) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Collection item not found.",
            detail: $"No collection item with ID '{itemId}' was found.");
}
