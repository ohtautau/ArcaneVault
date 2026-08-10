// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.CollectionItems;

[Authorize]
public class IndexModel(ICollectionItemApiClient collectionItemApiClient)
    : CollectionItemPageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public IReadOnlyList<CollectionItemResponse> Items { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var result = await collectionItemApiClient.GetAllAsync(Search, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Items = result.Value;
            return Page();
        }

        return HandleApiFailure(
                result.StatusCode,
                result.Problem,
                "Collection items could not be loaded.")
            ?? Page();
    }
}
