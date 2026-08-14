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
    public IReadOnlyList<TradePartnerResponse> Partners { get; private set; } = [];
    public IReadOnlyList<CollectionItemResponse> OfferedItems { get; private set; } = [];
    public IReadOnlyList<CollectionItemResponse> RequestedItems { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int? wishlistItemId, string? recipientUserName, CancellationToken ct)
    {
        Input.WishlistItemId = wishlistItemId;
        Input.RecipientUserName = recipientUserName ?? string.Empty;
        return await LoadAsync(ct);
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return await LoadAsync(ct);
        var result = await exchangeClient.CreateTradeAsync(Input, ct);
        if (result.IsSuccess) return RedirectToPage("Index");
        if ((int)result.StatusCode is 401) return Challenge();
        if ((int)result.StatusCode is 403) return Forbid();
        ModelState.AddModelError(string.Empty, result.Problem?.Detail ?? result.Problem?.Title ?? "Trade request could not be sent.");
        return await LoadAsync(ct);
    }
    private async Task<IActionResult> LoadAsync(CancellationToken ct)
    {
        var partners = await exchangeClient.GetTradePartnersAsync(ct);
        if (partners.IsSuccess && partners.Value is not null) Partners = partners.Value;
        if (Input.WishlistItemId is int wishlistItemId)
        {
            var wish = await exchangeClient.GetWishlistItemAsync(wishlistItemId, ct);
            if (!wish.IsSuccess || wish.Value is null) return NotFound();
            if (string.Equals(wish.Value.UserName, User.Identity!.Name, StringComparison.OrdinalIgnoreCase)) return BadRequest("You cannot trade against your own wishlist.");
            WishlistItem = wish.Value;
            Input.RecipientUserName = wish.Value.UserName;
        }
        var ownItems = await collectionClient.GetAllAsync(null, ct);
        if (!ownItems.IsSuccess || ownItems.Value is null) return Page();
        OfferedItems = ownItems.Value.Where(item => string.Equals(item.UserName, User.Identity!.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(Input.RecipientUserName))
        {
            var theirItems = await collectionClient.GetForTradeAsync(Input.RecipientUserName, ct);
            if (theirItems.IsSuccess && theirItems.Value is not null) RequestedItems = theirItems.Value;
        }
        return Page();
    }
}
