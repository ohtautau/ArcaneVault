// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.Categories;

[Authorize(Roles = "Staff")]
public class DetailsModel(ICategoryApiClient categoryApiClient) : CategoryPageModel
{
    public CategoryResponse Category { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(
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
