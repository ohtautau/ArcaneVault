// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;

namespace ArcaneVault.Services;

public interface IWishlistService
{
    Task<IReadOnlyList<WishlistItemResponse>> GetAllAsync(string? search, string? excludeUserName, CancellationToken cancellationToken = default);
    Task<WishlistResult> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WishlistResult> CreateAsync(string userName, CreateWishlistItemRequest request, CancellationToken cancellationToken = default);
    Task<WishlistResult> UpdateAsync(int id, string userName, UpdateWishlistItemRequest request, CancellationToken cancellationToken = default);
    Task<WishlistResult> DeleteAsync(int id, string userName, CancellationToken cancellationToken = default);
}
