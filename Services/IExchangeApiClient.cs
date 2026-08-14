// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;

namespace ArcaneVault.Services;

public interface IExchangeApiClient
{
    Task<ApiResult<IReadOnlyList<WishlistItemResponse>>> GetWishlistAsync(string? search, bool othersOnly, CancellationToken cancellationToken = default);
    Task<ApiResult<WishlistItemResponse>> GetWishlistItemAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult<WishlistItemResponse>> CreateWishlistItemAsync(CreateWishlistItemRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<WishlistItemResponse>> UpdateWishlistItemAsync(int id, UpdateWishlistItemRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteWishlistItemAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<TradeResponse>>> GetTradesAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<TradeResponse>>> GetActiveTradesAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<TradeResponse>> CreateTradeAsync(CreateTradeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<TradeResponse>> UpdateTradeStatusAsync(int id, string status, CancellationToken cancellationToken = default);
    Task<ApiResult<TradeResponse>> StaffCancelTradeAsync(int id, string note, CancellationToken cancellationToken = default);
}
