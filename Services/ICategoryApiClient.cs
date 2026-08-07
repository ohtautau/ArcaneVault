// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;

namespace ArcaneVault.Services;

public interface ICategoryApiClient
{
    Task<ApiResult<IReadOnlyList<CategoryResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<CategoryResponse>> GetByCodeAsync(
        string categoryCode,
        CancellationToken cancellationToken = default);

    Task<ApiResult<CategoryResponse>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<CategoryResponse>> UpdateAsync(
        string categoryCode,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAsync(
        string categoryCode,
        CancellationToken cancellationToken = default);
}
