// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Data;
using ArcaneVault.Models.Entities;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.Services;

public class CategoryService(ArcaneVaultDbContext dbContext) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryCode)
            .Select(category => new CategoryResponse
            {
                CategoryCode = category.CategoryCode,
                CategoryName = category.CategoryName,
                CollectionItemCount = category.CollectionItemCategories.Count
            })
            .ToListAsync(cancellationToken);

    public async Task<CategoryResult> GetByCodeAsync(
        string categoryCode,
        CancellationToken cancellationToken = default)
    {
        var response = await FindResponseAsync(categoryCode, cancellationToken);

        return response is null
            ? new CategoryResult(CategoryStatus.NotFound)
            : new CategoryResult(CategoryStatus.Success, response);
    }

    public async Task<CategoryResult> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var categoryCode = NormalizeCode(request.CategoryCode);
        var categoryName = request.CategoryName.Trim();

        if (await CodeExistsAsync(categoryCode, cancellationToken))
        {
            return new CategoryResult(CategoryStatus.DuplicateCode);
        }

        if (await NameExistsAsync(categoryName, null, cancellationToken))
        {
            return new CategoryResult(CategoryStatus.DuplicateName);
        }

        var category = new Category
        {
            CategoryCode = categoryCode,
            CategoryName = categoryName
        };
        dbContext.Categories.Add(category);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            dbContext.Entry(category).State = EntityState.Detached;
            return await ResolveUniqueConflictAsync(
                categoryCode,
                categoryName,
                null,
                cancellationToken);
        }

        return new CategoryResult(
            CategoryStatus.Success,
            new CategoryResponse
            {
                CategoryCode = category.CategoryCode,
                CategoryName = category.CategoryName,
                CollectionItemCount = 0
            });
    }

    public async Task<CategoryResult> UpdateAsync(
        string categoryCode,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        categoryCode = NormalizeCode(categoryCode);
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(
                existing => existing.CategoryCode == categoryCode,
                cancellationToken);

        if (category is null)
        {
            return new CategoryResult(CategoryStatus.NotFound);
        }

        var categoryName = request.CategoryName.Trim();
        if (await NameExistsAsync(categoryName, categoryCode, cancellationToken))
        {
            return new CategoryResult(CategoryStatus.DuplicateName);
        }

        category.CategoryName = categoryName;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return await ResolveUniqueConflictAsync(
                categoryCode,
                categoryName,
                categoryCode,
                cancellationToken);
        }

        var response = await FindResponseAsync(categoryCode, cancellationToken);
        return new CategoryResult(CategoryStatus.Success, response);
    }

    public async Task<CategoryResult> DeleteAsync(
        string categoryCode,
        CancellationToken cancellationToken = default)
    {
        categoryCode = NormalizeCode(categoryCode);
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(
                existing => existing.CategoryCode == categoryCode,
                cancellationToken);

        if (category is null)
        {
            return new CategoryResult(CategoryStatus.NotFound);
        }

        var isInUse = await dbContext.CollectionItemCategories
            .IgnoreQueryFilters()
            .AnyAsync(
                link => link.CategoryCode == categoryCode,
                cancellationToken);

        if (isInUse)
        {
            return new CategoryResult(CategoryStatus.InUse);
        }

        dbContext.Categories.Remove(category);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            dbContext.Entry(category).State = EntityState.Unchanged;
            return new CategoryResult(CategoryStatus.InUse);
        }

        return new CategoryResult(CategoryStatus.Success);
    }

    private Task<CategoryResponse?> FindResponseAsync(
        string categoryCode,
        CancellationToken cancellationToken) =>
        dbContext.Categories
            .AsNoTracking()
            .Where(category => category.CategoryCode == NormalizeCode(categoryCode))
            .Select(category => new CategoryResponse
            {
                CategoryCode = category.CategoryCode,
                CategoryName = category.CategoryName,
                CollectionItemCount = category.CollectionItemCategories.Count
            })
            .SingleOrDefaultAsync(cancellationToken);

    private Task<bool> CodeExistsAsync(
        string categoryCode,
        CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(
            category => category.CategoryCode == categoryCode,
            cancellationToken);

    private Task<bool> NameExistsAsync(
        string categoryName,
        string? excludedCategoryCode,
        CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(
            category => category.CategoryName == categoryName
                && (excludedCategoryCode == null
                    || category.CategoryCode != excludedCategoryCode),
            cancellationToken);

    private async Task<CategoryResult> ResolveUniqueConflictAsync(
        string categoryCode,
        string categoryName,
        string? excludedCategoryCode,
        CancellationToken cancellationToken)
    {
        if (excludedCategoryCode is null
            && await CodeExistsAsync(categoryCode, cancellationToken))
        {
            return new CategoryResult(CategoryStatus.DuplicateCode);
        }

        if (await NameExistsAsync(
            categoryName,
            excludedCategoryCode,
            cancellationToken))
        {
            return new CategoryResult(CategoryStatus.DuplicateName);
        }

        return new CategoryResult(CategoryStatus.Conflict);
    }

    private static string NormalizeCode(string categoryCode) =>
        categoryCode.Trim().ToUpperInvariant();
}
