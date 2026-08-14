// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;

namespace ArcaneVault.Services;

public interface IExchangeApiClient
{
    Task<ApiResult<IReadOnlyList<WishlistItemResponse>>> GetWishlistAsync(string? userName, bool othersOnly, CancellationToken cancellationToken = default);
    Task<ApiResult<WishlistItemResponse>> GetWishlistItemAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult<WishlistItemResponse>> CreateWishlistItemAsync(CreateWishlistItemRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<WishlistItemResponse>> UpdateWishlistItemAsync(int id, UpdateWishlistItemRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteWishlistItemAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<TradeRequestResponse>>> GetTradeRequestsAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<TradeRequestResponse>> CreateTradeRequestAsync(CreateTradeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<TradeRequestResponse>> UpdateTradeStatusAsync(int id, string status, CancellationToken cancellationToken = default);
}
