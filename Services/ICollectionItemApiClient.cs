// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;

namespace ArcaneVault.Services;

public interface ICollectionItemApiClient
{
    Task<ApiResult<IReadOnlyList<ItemTypeResponse>>> GetItemTypesAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<CollectionItemResponse>>> GetAllAsync(
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<CollectionItemResponse>>> GetAllAsync(
        string? search,
        int changePeriodDays,
        CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<CollectionItemResponse>>> GetForTradeAsync(
        string userName,
        CancellationToken cancellationToken = default);

    Task<ApiResult<CollectionItemResponse>> GetByIdAsync(
        int itemId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<CollectionItemResponse>> CreateAsync(
        CreateCollectionItemRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<CollectionItemResponse>> UpdateAsync(
        int itemId,
        UpdateCollectionItemRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<CollectionItemResponse>> UploadImageAsync(
        int itemId,
        IFormFile image,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAsync(
        int itemId,
        CancellationToken cancellationToken = default);
}
