// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArcaneVault.Pages.TradeRequests;

[Authorize]
public class CreateModel(IExchangeApiClient exchangeClient, ICollectionItemApiClient collectionClient) : PageModel
{
    [BindProperty] public CreateTradeRequest Input { get; set; } = new();
    public WishlistItemResponse WishlistItem { get; private set; } = new();
    public IReadOnlyList<CollectionItemResponse> CollectionItems { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int wishlistItemId, CancellationToken ct)
    {
        Input.WishlistItemId = wishlistItemId;
        return await LoadAsync(ct);
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return await LoadAsync(ct);
        var result = await exchangeClient.CreateTradeRequestAsync(Input, ct);
        if (result.IsSuccess) return RedirectToPage("Index");
        if ((int)result.StatusCode is 401) return Challenge();
        if ((int)result.StatusCode is 403) return Forbid();
        ModelState.AddModelError(string.Empty, result.Problem?.Detail ?? result.Problem?.Title ?? "Trade request could not be sent.");
        return await LoadAsync(ct);
    }
    private async Task<IActionResult> LoadAsync(CancellationToken ct)
    {
        var wish = await exchangeClient.GetWishlistItemAsync(Input.WishlistItemId, ct);
        if (!wish.IsSuccess || wish.Value is null) return NotFound();
        if (string.Equals(wish.Value.UserName, User.Identity!.Name, StringComparison.OrdinalIgnoreCase)) return BadRequest("You cannot trade against your own wishlist.");
        WishlistItem = wish.Value;
        var items = await collectionClient.GetAllAsync(null, ct);
        if (!items.IsSuccess || items.Value is null) return items.StatusCode == System.Net.HttpStatusCode.Unauthorized ? Challenge() : Page();
        CollectionItems = items.Value.Where(item => string.Equals(item.UserName, User.Identity!.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        return Page();
    }
}
