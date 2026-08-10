// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;

namespace ArcaneVault.Services;

public interface ICollectionItemService
{
    Task<IReadOnlyList<CollectionItemResponse>> GetAllAsync(
        string userName,
        bool isStaff,
        string? search,
        CancellationToken cancellationToken = default);

    Task<CollectionItemResult> GetByIdAsync(
        int itemId,
        string userName,
        bool isStaff,
        CancellationToken cancellationToken = default);

    Task<CollectionItemResult> CreateAsync(
        string userName,
        CreateCollectionItemRequest request,
        CancellationToken cancellationToken = default);

    Task<CollectionItemResult> UpdateAsync(
        int itemId,
        string userName,
        bool isStaff,
        UpdateCollectionItemRequest request,
        CancellationToken cancellationToken = default);

    Task<CollectionItemResult> DeleteAsync(
        int itemId,
        string userName,
        bool isStaff,
        CancellationToken cancellationToken = default);
}
