// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.CollectionItems;

[Authorize]
public class DetailsModel(ICollectionItemApiClient collectionItemApiClient)
    : CollectionItemPageModel
{
    public CollectionItemResponse Item { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        var result = await collectionItemApiClient.GetByIdAsync(itemId, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Item = result.Value;
            return Page();
        }

        return HandleApiFailure(
                result.StatusCode,
                result.Problem,
                "The collection item could not be loaded.")
            ?? Page();
    }
}
