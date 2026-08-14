using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace ArcaneVault.Pages.Staff;
[Authorize(Roles = "Staff")]
public class TradesModel(IExchangeApiClient client) : PageModel
{
    public IReadOnlyList<TradeResponse> ActiveTrades { get; private set; } = [];
    public IReadOnlyList<TradeResponse> TradeHistory { get; private set; } = [];
    [BindProperty] public string ResolutionNote { get; set; } = string.Empty;
    public async Task OnGetAsync(CancellationToken ct) => await Load(ct);
    public async Task<IActionResult> OnPostCancelAsync(int id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ResolutionNote) || ResolutionNote.Trim().Length < 5) ModelState.AddModelError(nameof(ResolutionNote), "Enter a resolution note of at least 5 characters.");
        else { var result = await client.StaffCancelTradeAsync(id, ResolutionNote, ct); if (result.IsSuccess) return RedirectToPage(); ModelState.AddModelError(string.Empty, result.Problem?.Detail ?? "Trade could not be cancelled."); }
        await Load(ct); return Page();
    }
    public async Task<IActionResult> OnPostResolveAsync(int id, string resolution, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ResolutionNote) || ResolutionNote.Trim().Length < 5) ModelState.AddModelError(nameof(ResolutionNote), "Enter a resolution note of at least 5 characters.");
        else { var result = await client.StaffResolveTradeAsync(id, resolution, ResolutionNote, ct); if (result.IsSuccess) return RedirectToPage(); ModelState.AddModelError(string.Empty, result.Problem?.Detail ?? "Trade could not be resolved."); }
        await Load(ct); return Page();
    }
    private async Task Load(CancellationToken ct)
    {
        var result = await client.GetAllStaffTradesAsync(ct);
        if (!result.IsSuccess || result.Value is null) return;
        ActiveTrades = result.Value.Where(t => t.Status is "Pending" or "Trading" or "Disputed").ToList();
        TradeHistory = result.Value.Where(t => t.Status is not ("Pending" or "Trading" or "Disputed")).ToList();
    }
}
