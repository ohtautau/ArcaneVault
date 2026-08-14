// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;

namespace ArcaneVault.Services;

public interface ITradeRequestService
{
    Task<IReadOnlyList<TradeRequestResponse>> GetForUserAsync(string userName, CancellationToken cancellationToken = default);
    Task<TradeRequestResult> CreateAsync(string userName, CreateTradeRequest request, CancellationToken cancellationToken = default);
    Task<TradeRequestResult> UpdateStatusAsync(int id, string userName, string status, CancellationToken cancellationToken = default);
}
