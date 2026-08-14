// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.Wishlist;

[Authorize]
public class EditModel(IExchangeApiClient client) : WishlistPageModel
{
    [BindProperty] public UpdateWishlistItemRequest Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var result = await client.GetWishlistItemAsync(Id, ct);
        if (!result.IsSuccess || result.Value is null) return HandleFailure(result.StatusCode, result.Problem, "Wishlist item not found.") ?? Page();
        if (!string.Equals(result.Value.UserName, User.Identity!.Name, StringComparison.OrdinalIgnoreCase)) return Forbid();
        Input = new() { ItemName = result.Value.ItemName, Notes = result.Value.Notes, DesiredQuantity = result.Value.DesiredQuantity }; return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        var result = await client.UpdateWishlistItemAsync(Id, Input, ct);
        if (result.IsSuccess) return RedirectToPage("Index");
        return HandleFailure(result.StatusCode, result.Problem, "Wishlist item could not be updated.") ?? Page();
    }
}
