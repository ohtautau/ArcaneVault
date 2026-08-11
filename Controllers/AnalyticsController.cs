// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Controllers;

[ApiController]
[Authorize(Roles = "Staff")]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AnalyticsDashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnalyticsDashboardResponse>> GetDashboard(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days is not (7 or 30 or 90))
        {
            ModelState.AddModelError(nameof(days), "Days must be 7, 30, or 90.");
            return ValidationProblem(ModelState);
        }

        return Ok(await analyticsService.GetDashboardAsync(days, cancellationToken));
    }
}
