// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArcaneVault.Pages.TradeRequests;

[Authorize]
public class IndexModel(IExchangeApiClient client) : PageModel
{
    public IReadOnlyList<TradeResponse> Incoming { get; private set; } = [];
    public IReadOnlyList<TradeResponse> Outgoing { get; private set; } = [];
    public IReadOnlyList<TradeResponse> Pending { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { await LoadAsync(ct); return Page(); }
    public async Task<IActionResult> OnPostStatusAsync(int id, string status, CancellationToken ct)
    {
        var result = await client.UpdateTradeStatusAsync(id, status, null, ct);
        if (result.IsSuccess) return RedirectToPage();
        if ((int)result.StatusCode == 403) return Forbid();
        ModelState.AddModelError(string.Empty, result.Problem?.Detail ?? result.Problem?.Title ?? "Trade request could not be updated.");
        await LoadAsync(ct); return Page();
    }
    public async Task<IActionResult> OnPostDisputeAsync(int id, string disputeReason, CancellationToken ct)
    {
        var result = await client.UpdateTradeStatusAsync(id, "Disputed", disputeReason, ct);
        if (result.IsSuccess) return RedirectToPage();
        ModelState.AddModelError(string.Empty, result.Problem?.Detail ?? "The dispute could not be submitted.");
        await LoadAsync(ct); return Page();
    }
    private async Task LoadAsync(CancellationToken ct)
    {
        var result = await client.GetTradesAsync(ct);
        if (!result.IsSuccess || result.Value is null) { ModelState.AddModelError(string.Empty, result.Problem?.Detail ?? "Trade requests could not be loaded."); return; }
        Incoming = result.Value.Where(item => string.Equals(item.RecipientUserName, User.Identity!.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        Outgoing = result.Value.Where(item => string.Equals(item.RequesterUserName, User.Identity!.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        Pending = result.Value.Where(item => item.Status is "Pending" or "Trading" or "Disputed").ToList();
    }
}
