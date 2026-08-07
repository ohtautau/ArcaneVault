// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.Categories;

[Authorize(Roles = "Staff")]
public class EditModel(ICategoryApiClient categoryApiClient) : CategoryPageModel
{
    [BindProperty]
    public UpdateCategoryRequest Input { get; set; } = new();

    public string CategoryCode { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(
        string categoryCode,
        CancellationToken cancellationToken)
    {
        var result = await categoryApiClient.GetByCodeAsync(
            categoryCode,
            cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            CategoryCode = result.Value.CategoryCode;
            Input.CategoryName = result.Value.CategoryName;
            return Page();
        }

        return HandleApiFailure(
                result.StatusCode,
                result.Problem,
                "The category could not be loaded.")
            ?? Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string categoryCode,
        CancellationToken cancellationToken)
    {
        CategoryCode = categoryCode;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await categoryApiClient.UpdateAsync(
            categoryCode,
            Input,
            cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return RedirectToPage(
                "Details",
                new { categoryCode = result.Value.CategoryCode });
        }

        return HandleApiFailure(
                result.StatusCode,
                result.Problem,
                "The category could not be updated.")
            ?? Page();
    }
}
