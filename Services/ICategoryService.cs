// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;

namespace ArcaneVault.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<CategoryResult> GetByCodeAsync(
        string categoryCode,
        CancellationToken cancellationToken = default);

    Task<CategoryResult> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<CategoryResult> UpdateAsync(
        string categoryCode,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<CategoryResult> DeleteAsync(
        string categoryCode,
        CancellationToken cancellationToken = default);
}
