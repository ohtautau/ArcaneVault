// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.Categories;

[Authorize(Roles = "Staff")]
public class DeleteModel(ICategoryApiClient categoryApiClient) : CategoryPageModel
{
    public CategoryResponse Category { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(
        string categoryCode,
        CancellationToken cancellationToken) =>
        await LoadCategoryAsync(categoryCode, cancellationToken);

    public async Task<IActionResult> OnPostAsync(
        string categoryCode,
        CancellationToken cancellationToken)
    {
        var result = await categoryApiClient.DeleteAsync(
            categoryCode,
            cancellationToken);

        if (result.IsSuccess)
        {
            return RedirectToPage("Index");
        }

        var failure = HandleApiFailure(
            result.StatusCode,
            result.Problem,
            "The category could not be deleted.");
        if (failure is not null)
        {
            return failure;
        }

        return await LoadCategoryAsync(categoryCode, cancellationToken);
    }

    private async Task<IActionResult> LoadCategoryAsync(
        string categoryCode,
        CancellationToken cancellationToken)
    {
        var result = await categoryApiClient.GetByCodeAsync(
            categoryCode,
            cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            Category = result.Value;
            return Page();
        }

        return HandleApiFailure(
                result.StatusCode,
                result.Problem,
                "The category could not be loaded.")
            ?? Page();
    }
}
