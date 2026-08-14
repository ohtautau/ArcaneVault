// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;

namespace ArcaneVault.Services;

public interface IAnalyticsApiClient
{
    Task<ApiResult<AnalyticsDashboardResponse>> GetDashboardAsync(
        int days,
        CancellationToken cancellationToken = default);
}
