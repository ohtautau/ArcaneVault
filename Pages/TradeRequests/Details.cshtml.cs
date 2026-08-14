using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArcaneVault.Pages.TradeRequests;

[Authorize]
public class DetailsModel(IExchangeApiClient client) : PageModel
{
    public TradeResponse Trade { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        var result = await client.GetTradeAsync(id, ct);
        if (result.IsSuccess && result.Value is not null) { Trade = result.Value; return Page(); }
        if ((int)result.StatusCode == 403) return Forbid();
        if ((int)result.StatusCode == 404) return NotFound();
        return StatusCode((int)result.StatusCode);
    }
}
