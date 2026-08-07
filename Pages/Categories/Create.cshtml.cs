// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.Categories;

[Authorize(Roles = "Staff")]
public class CreateModel(ICategoryApiClient categoryApiClient) : CategoryPageModel
{
    [BindProperty]
    public CreateCategoryRequest Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await categoryApiClient.CreateAsync(Input, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return RedirectToPage(
                "Details",
                new { categoryCode = result.Value.CategoryCode });
        }

        return HandleApiFailure(
                result.StatusCode,
                result.Problem,
                "The category could not be created.")
            ?? Page();
    }
}
