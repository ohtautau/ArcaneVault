// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.Wishlist;

[Authorize]
public class IndexModel(IExchangeApiClient exchangeApiClient) : WishlistPageModel
{
    [BindProperty] public CreateWishlistItemRequest Input { get; set; } = new();
    public IReadOnlyList<WishlistItemResponse> Items { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { await LoadAsync(ct); return Page(); }
    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await exchangeApiClient.CreateWishlistItemAsync(Input, ct);
            if (result.IsSuccess) return RedirectToPage();
            HandleFailure(result.StatusCode, result.Problem, "The wishlist item could not be added.");
        }
        await LoadAsync(ct); return Page();
    }
    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var result = await exchangeApiClient.DeleteWishlistItemAsync(id, ct);
        if (result.IsSuccess) return RedirectToPage();
        HandleFailure(result.StatusCode, result.Problem, "The wishlist item could not be deleted.");
        await LoadAsync(ct); return Page();
    }
    private async Task LoadAsync(CancellationToken ct)
    {
        var result = await exchangeApiClient.GetWishlistAsync(User.Identity!.Name, false, ct);
        if (result.IsSuccess && result.Value is not null) Items = result.Value;
        else HandleFailure(result.StatusCode, result.Problem, "Your wishlist could not be loaded.");
    }
}
