// Name:
// Student Admin No.:
// Tutorial Group:

using System.ComponentModel.DataAnnotations;
using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArcaneVault.Pages.Staff;

[Authorize(Roles = "Staff")]
public class AnalyticsModel(IAnalyticsApiClient analyticsApiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    [Range(7, 90)]
    public int Days { get; set; } = 30;

    public AnalyticsDashboardResponse Dashboard { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Days is not (7 or 30 or 90))
        {
            Days = 30;
        }

        var result = await analyticsApiClient.GetDashboardAsync(Days, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Dashboard = result.Value;
            return Page();
        }

        if ((int)result.StatusCode == StatusCodes.Status401Unauthorized) return Challenge();
        if ((int)result.StatusCode == StatusCodes.Status403Forbidden) return Forbid();

        ModelState.AddModelError(
            string.Empty,
            result.Problem?.Detail ?? "Analytics data could not be loaded.");
        return Page();
    }
}
