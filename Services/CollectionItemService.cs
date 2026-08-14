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
    public async Task<IReadOnlyList<ItemTypeResponse>> GetItemTypesAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ItemTypes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{EscapeLikePattern(search.Trim())}%";
            query = query.Where(type => EF.Functions.Like(type.ItemTypeId, term, "\\") || EF.Functions.Like(type.ItemName, term, "\\"));
        }
        return await query.OrderBy(type => type.ItemName).Select(type => new ItemTypeResponse { ItemTypeId = type.ItemTypeId, ItemName = type.ItemName }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CollectionItemResponse>> GetAllAsync(
        string userName,
        bool isStaff,
        string? search,
        int changePeriodDays,
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
                || EF.Functions.Like(item.ItemTypeId, likePattern, "\\")
                || (isNumber && (item.ItemId == number
                    || item.StartingQuantity == number
                    || item.CurrentQuantity == number))
                || item.CollectionItemCategories.Any(link =>
                    EF.Functions.Like(link.CategoryCode, likePattern, "\\")
                    || EF.Functions.Like(link.Category.CategoryName, likePattern, "\\")));
        }

        var items = await ProjectResponses(query)
            .OrderBy(item => item.ItemName)
            .ThenBy(item => item.ItemId)
            .ToListAsync(cancellationToken);

        var days = changePeriodDays is 1 or 3 or 7 or 30 ? changePeriodDays : 7;
        if (items.Count == 0) return items;
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var itemIds = items.Select(item => item.ItemId).ToList();
        var history = await dbContext.CollectionItemQuantityHistory.AsNoTracking()
            .Where(point => itemIds.Contains(point.ItemId))
            .OrderBy(point => point.ChangedAtUtc)
            .Select(point => new { point.ItemId, point.Quantity, point.ChangedAtUtc })
            .ToListAsync(cancellationToken);
        var historyByItem = history.GroupBy(point => point.ItemId).ToDictionary(group => group.Key, group => group.ToList());
        foreach (var item in items)
        {
            item.ChangePeriodDays = days;
            if (!historyByItem.TryGetValue(item.ItemId, out var points) || points.Count == 0) continue;
            var baseline = points.LastOrDefault(point => point.ChangedAtUtc <= cutoff);
            item.RecentQuantityChange = item.CurrentQuantity - (baseline ?? points[0]).Quantity;
        }
        return items;
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

        response.QuantityHistory = await dbContext.CollectionItemQuantityHistory
            .AsNoTracking()
            .Where(history => history.ItemId == itemId)
            .OrderByDescending(history => history.ChangedAtUtc)
            .Take(30)
            .OrderBy(history => history.ChangedAtUtc)
            .Select(history => new QuantityHistoryPointResponse
            {
                Quantity = history.Quantity,
                ChangedAtUtc = history.ChangedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new CollectionItemResult(CollectionItemStatus.Success, response);
    }

    public async Task<CollectionItemResult> CreateAsync(
        string userName,
        CreateCollectionItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestedTypeId = request.ItemTypeId?.Trim().ToUpperInvariant();
        ItemType itemType;
        if (request.IsNewItemType)
        {
            requestedTypeId = string.IsNullOrWhiteSpace(requestedTypeId) ? $"ITEM-{Guid.NewGuid():N}"[..13].ToUpperInvariant() : requestedTypeId;
            if (await dbContext.ItemTypes.AnyAsync(type => type.ItemTypeId == requestedTypeId, cancellationToken))
                return new CollectionItemResult(CollectionItemStatus.InvalidItemType);
            itemType = new ItemType { ItemTypeId = requestedTypeId, ItemName = request.ItemName.Trim() };
            dbContext.ItemTypes.Add(itemType);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(requestedTypeId)) return new CollectionItemResult(CollectionItemStatus.InvalidItemType);
            itemType = await dbContext.ItemTypes.SingleOrDefaultAsync(type => type.ItemTypeId == requestedTypeId, cancellationToken)
                ?? null!;
            if (itemType is null) return new CollectionItemResult(CollectionItemStatus.InvalidItemType);
        }
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
            ItemTypeId = itemType.ItemTypeId,
            ItemName = itemType.ItemName,
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
        if (request.CurrentQuantity < item.LockedQuantity)
        {
            return new CollectionItemResult(CollectionItemStatus.Conflict);
        }
        // Item names belong to the permanent item type and cannot be changed per inventory record.
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

        if (item.IsInTrade)
        {
            return new CollectionItemResult(CollectionItemStatus.Conflict);
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
            ItemTypeId = item.ItemTypeId,
            ItemName = item.ItemName,
            StartingQuantity = item.StartingQuantity,
            CurrentQuantity = item.CurrentQuantity,
            LockedQuantity = item.LockedQuantity,
            IsInTrade = item.IsInTrade,
            UserName = item.UserName,
            Categories = item.CollectionItemCategories
                .OrderBy(link => link.CategoryCode)
                .Select(link => new CollectionItemCategoryResponse
                {
                    CategoryCode = link.CategoryCode,
                    CategoryName = link.Category.CategoryName
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
