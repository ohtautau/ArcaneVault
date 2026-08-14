// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;

namespace ArcaneVault.Services;

public interface IAnalyticsService
{
    Task<AnalyticsDashboardResponse> GetDashboardAsync(
        int days,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCollectionsCsvAsync(CancellationToken cancellationToken = default);
}
