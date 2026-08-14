// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Data;
using ArcaneVault.Models.Entities;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.Services;

public class CollectionItemService(ArcaneVaultDbContext dbContext)
    : ICollectionItemService
{
    public async Task<IReadOnlyList<CollectionItemResponse>> GetAllAsync(
        string userName,
        bool isStaff,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CollectionItems
            .AsNoTracking()
            .Where(item => isStaff || item.UserName == userName);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            var likePattern = $"%{EscapeLikePattern(searchTerm)}%";
            var isNumber = int.TryParse(searchTerm, out var number);

            query = query.Where(item =>
                EF.Functions.Like(item.ItemName, likePattern, "\\")
                || EF.Functions.Like(item.UserName, likePattern, "\\")
                || (isNumber && (item.ItemId == number
                    || item.StartingQuantity == number
                    || item.CurrentQuantity == number))
                || item.CollectionItemCategories.Any(link =>
                    EF.Functions.Like(link.CategoryCode, likePattern, "\\")
                    || EF.Functions.Like(link.Category.CategoryName, likePattern, "\\")));
        }

        return await ProjectResponses(query)
            .OrderBy(item => item.ItemName)
            .ThenBy(item => item.ItemId)
            .ToListAsync(cancellationToken);
    }

    public async Task<CollectionItemResult> GetByIdAsync(
        int itemId,
        string userName,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.CollectionItems
            .AsNoTracking()
            .SingleOrDefaultAsync(existing => existing.ItemId == itemId, cancellationToken);

        if (item is null)
        {
            return new CollectionItemResult(CollectionItemStatus.NotFound);
        }

        if (!isStaff && !string.Equals(item.UserName, userName, StringComparison.OrdinalIgnoreCase))
        {
            return new CollectionItemResult(CollectionItemStatus.Forbidden);
        }

        var response = await ProjectResponses(
                dbContext.CollectionItems
                    .AsNoTracking()
                    .Where(existing => existing.ItemId == itemId))
            .SingleAsync(cancellationToken);

        return new CollectionItemResult(CollectionItemStatus.Success, response);
    }

    public async Task<CollectionItemResult> CreateAsync(
        string userName,
        CreateCollectionItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var categoryCodes = NormalizeCategoryCodes(request.CategoryCodes);
        var invalidCodes = await GetInvalidCategoryCodesAsync(categoryCodes, cancellationToken);
        if (invalidCodes.Count > 0)
        {
            return new CollectionItemResult(
                CollectionItemStatus.InvalidCategories,
                InvalidCategoryCodes: invalidCodes);
        }

        var item = new CollectionItem
        {
            ItemName = request.ItemName.Trim(),
            StartingQuantity = request.StartingQuantity,
            CurrentQuantity = request.StartingQuantity,
            UserName = userName,
            CollectionItemCategories = categoryCodes
                .Select(code => new CollectionItemCategory { CategoryCode = code })
                .ToList()
        };
        dbContext.CollectionItems.Add(item);
        item.QuantityHistory.Add(new CollectionItemQuantityHistory
        {
            Quantity = item.CurrentQuantity,
            ChangedAtUtc = DateTime.UtcNow
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new CollectionItemResult(CollectionItemStatus.Conflict);
        }

        return await GetByIdAsync(item.ItemId, userName, false, cancellationToken);
    }

    public async Task<CollectionItemResult> UpdateAsync(
        int itemId,
        string userName,
        bool isStaff,
        UpdateCollectionItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.CollectionItems
            .Include(existing => existing.CollectionItemCategories)
            .SingleOrDefaultAsync(existing => existing.ItemId == itemId, cancellationToken);

        if (item is null)
        {
            return new CollectionItemResult(CollectionItemStatus.NotFound);
        }

        if (!isStaff && !string.Equals(item.UserName, userName, StringComparison.OrdinalIgnoreCase))
        {
            return new CollectionItemResult(CollectionItemStatus.Forbidden);
        }

        var categoryCodes = NormalizeCategoryCodes(request.CategoryCodes);
        var invalidCodes = await GetInvalidCategoryCodesAsync(categoryCodes, cancellationToken);
        if (invalidCodes.Count > 0)
        {
            return new CollectionItemResult(
                CollectionItemStatus.InvalidCategories,
                InvalidCategoryCodes: invalidCodes);
        }

        var quantityChanged = item.CurrentQuantity != request.CurrentQuantity;
        item.ItemName = request.ItemName.Trim();
        item.CurrentQuantity = request.CurrentQuantity;
        if (quantityChanged)
        {
            item.QuantityHistory.Add(new CollectionItemQuantityHistory
            {
                ItemId = itemId,
                Quantity = request.CurrentQuantity,
                ChangedAtUtc = DateTime.UtcNow
            });
        }

        var requestedCodes = categoryCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedLinks = item.CollectionItemCategories
            .Where(link => !requestedCodes.Contains(link.CategoryCode))
            .ToList();
        dbContext.CollectionItemCategories.RemoveRange(removedLinks);

        var existingCodes = item.CollectionItemCategories
            .Select(link => link.CategoryCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var categoryCode in categoryCodes.Where(code => !existingCodes.Contains(code)))
        {
            item.CollectionItemCategories.Add(new CollectionItemCategory
            {
                ItemId = itemId,
                CategoryCode = categoryCode
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new CollectionItemResult(CollectionItemStatus.Conflict);
        }

        return await GetByIdAsync(itemId, userName, isStaff, cancellationToken);
    }

    public async Task<CollectionItemResult> DeleteAsync(
        int itemId,
        string userName,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.CollectionItems
            .SingleOrDefaultAsync(existing => existing.ItemId == itemId, cancellationToken);

        if (item is null)
        {
            return new CollectionItemResult(CollectionItemStatus.NotFound);
        }

        if (!isStaff && !string.Equals(item.UserName, userName, StringComparison.OrdinalIgnoreCase))
        {
            return new CollectionItemResult(CollectionItemStatus.Forbidden);
        }

        item.IsDeleted = true;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new CollectionItemResult(CollectionItemStatus.Conflict);
        }

        return new CollectionItemResult(CollectionItemStatus.Success);
    }

    private async Task<List<string>> GetInvalidCategoryCodesAsync(
        IReadOnlyList<string> categoryCodes,
        CancellationToken cancellationToken)
    {
        if (categoryCodes.Count == 0)
        {
            return ["(none)"];
        }

        var existingCodes = await dbContext.Categories
            .Where(category => categoryCodes.Contains(category.CategoryCode))
            .Select(category => category.CategoryCode)
            .ToListAsync(cancellationToken);

        return categoryCodes
            .Except(existingCodes, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IQueryable<CollectionItemResponse> ProjectResponses(
        IQueryable<CollectionItem> query) =>
        query.Select(item => new CollectionItemResponse
        {
            ItemId = item.ItemId,
            ItemName = item.ItemName,
            StartingQuantity = item.StartingQuantity,
            CurrentQuantity = item.CurrentQuantity,
            UserName = item.UserName,
            Categories = item.CollectionItemCategories
                .OrderBy(link => link.CategoryCode)
                .Select(link => new CollectionItemCategoryResponse
                {
                    CategoryCode = link.CategoryCode,
                    CategoryName = link.Category.CategoryName
                })
                .ToList(),
            QuantityHistory = item.QuantityHistory
                .OrderByDescending(history => history.ChangedAtUtc)
                .Take(30)
                .OrderBy(history => history.ChangedAtUtc)
                .Select(history => new QuantityHistoryPointResponse
                {
                    Quantity = history.Quantity,
                    ChangedAtUtc = history.ChangedAtUtc
                })
                .ToList()
        });

    private static IReadOnlyList<string> NormalizeCategoryCodes(
        IEnumerable<string> categoryCodes) =>
        categoryCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string EscapeLikePattern(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
