// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.Wishlist;

[Authorize]
public class BrowseModel(IExchangeApiClient client) : WishlistPageModel
{
    [BindProperty(SupportsGet = true)] public string? UserName { get; set; }
    public IReadOnlyList<WishlistItemResponse> Items { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var result = await client.GetWishlistAsync(UserName, true, ct);
        if (result.IsSuccess && result.Value is not null) Items = result.Value;
        else HandleFailure(result.StatusCode, result.Problem, "Wishlists could not be loaded.");
        return Page();
    }
}
