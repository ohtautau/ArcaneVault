// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.Categories;

[Authorize(Roles = "Staff")]
public class IndexModel(ICategoryApiClient categoryApiClient) : CategoryPageModel
{
    public IReadOnlyList<CategoryResponse> Categories { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var result = await categoryApiClient.GetAllAsync(cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            Categories = result.Value;
            return Page();
        }

        return HandleApiFailure(
                result.StatusCode,
                result.Problem,
                "Categories could not be loaded.")
            ?? Page();
    }
}
