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
[Authorize(Roles = "Staff")]
[Route("api/categories")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{categoryCode}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetByCode(
        string categoryCode,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.GetByCodeAsync(
            categoryCode,
            cancellationToken);

        return result.Status == CategoryStatus.Success && result.Response is not null
            ? Ok(result.Response)
            : CategoryNotFound(categoryCode);
    }

    [HttpPost]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.CreateAsync(request, cancellationToken);

        if (result.Status == CategoryStatus.Success && result.Response is not null)
        {
            return CreatedAtAction(
                nameof(GetByCode),
                new { categoryCode = result.Response.CategoryCode },
                result.Response);
        }

        return result.Status switch
        {
            CategoryStatus.DuplicateCode => ConflictResponse(
                "Category code already exists.",
                "Choose a different category code."),
            CategoryStatus.DuplicateName => ConflictResponse(
                "Category name already exists.",
                "Choose a different category name."),
            _ => ConflictResponse(
                "Category creation conflict.",
                "The category could not be created because the data changed. Try again.")
        };
    }

    [HttpPut("{categoryCode}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Update(
        string categoryCode,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.UpdateAsync(
            categoryCode,
            request,
            cancellationToken);

        if (result.Status == CategoryStatus.Success && result.Response is not null)
        {
            return Ok(result.Response);
        }

        return result.Status switch
        {
            CategoryStatus.NotFound => CategoryNotFound(categoryCode),
            CategoryStatus.DuplicateName => ConflictResponse(
                "Category name already exists.",
                "Choose a different category name."),
            _ => ConflictResponse(
                "Category update conflict.",
                "The category could not be updated because the data changed. Try again.")
        };
    }

    [HttpDelete("{categoryCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        string categoryCode,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.DeleteAsync(
            categoryCode,
            cancellationToken);

        return result.Status switch
        {
            CategoryStatus.Success => NoContent(),
            CategoryStatus.NotFound => CategoryNotFound(categoryCode),
            CategoryStatus.InUse => ConflictResponse(
                "Category is in use.",
                "Remove the category from all collection items before deleting it."),
            _ => ConflictResponse(
                "Category deletion conflict.",
                "The category could not be deleted because the data changed. Try again.")
        };
    }

    private ObjectResult CategoryNotFound(string categoryCode) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Category not found.",
            detail: $"No category with code '{categoryCode}' was found.");

    private ObjectResult ConflictResponse(string title, string detail) =>
        Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: title,
            detail: detail);
}
