// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Data;
using ArcaneVault.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.Services;

public class AnalyticsService(ArcaneVaultDbContext dbContext) : IAnalyticsService
{
    private static readonly int[] ValidRanges = [7, 30, 90];

    public async Task<AnalyticsDashboardResponse> GetDashboardAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        days = ValidRanges.Contains(days) ? days : 30;
        var today = DateTime.UtcNow.Date;
        var fromUtc = today.AddDays(-(days - 1));

        var totalUsers = await dbContext.ArcaneVaultUsers.CountAsync(cancellationToken);
        var totalItems = await dbContext.CollectionItems.CountAsync(cancellationToken);
        var totalCategories = await dbContext.Categories.CountAsync(cancellationToken);
        var totalCurrentQuantity = await dbContext.CollectionItems
            .SumAsync(item => (int?)item.CurrentQuantity, cancellationToken) ?? 0;
        var totalStartingQuantity = await dbContext.CollectionItems
            .SumAsync(item => (int?)item.StartingQuantity, cancellationToken) ?? 0;

        var userDates = await dbContext.ArcaneVaultUsers
            .AsNoTracking()
            .Where(user => user.CreatedAtUtc >= fromUtc)
            .Select(user => user.CreatedAtUtc.Date)
            .ToListAsync(cancellationToken);
        var itemDates = await dbContext.CollectionItems
            .AsNoTracking()
            .Where(item => item.CreatedAtUtc >= fromUtc)
            .Select(item => item.CreatedAtUtc.Date)
            .ToListAsync(cancellationToken);

        var trends = Enumerable.Range(0, days)
            .Select(offset => fromUtc.AddDays(offset))
            .Select(date => new AnalyticsTrendPointResponse
            {
                Date = date,
                NewUsers = userDates.Count(value => value == date),
                NewCollectionItems = itemDates.Count(value => value == date)
            })
            .ToList();

        var categories = await dbContext.Categories
            .AsNoTracking()
            .Select(category => new PopularCategoryResponse
            {
                CategoryCode = category.CategoryCode,
                CategoryName = category.CategoryName,
                CollectionItemCount = category.CollectionItemCategories.Count
            })
            .OrderByDescending(category => category.CollectionItemCount)
            .ThenBy(category => category.CategoryName)
            .Take(8)
            .ToListAsync(cancellationToken);
        foreach (var category in categories)
        {
            category.Percentage = totalItems == 0
                ? 0
                : Math.Round(category.CollectionItemCount * 100m / totalItems, 1);
        }

        var topCollectors = await dbContext.ArcaneVaultUsers
            .AsNoTracking()
            .Where(user => user.Role.RoleName == "User")
            .Select(user => new TopCollectorResponse
            {
                UserName = user.UserName,
                CollectionItemCount = user.CollectionItems.Count,
                CurrentQuantity = user.CollectionItems.Sum(item => (int?)item.CurrentQuantity) ?? 0
            })
            .OrderByDescending(user => user.CollectionItemCount)
            .ThenBy(user => user.UserName)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentUsers = await dbContext.ArcaneVaultUsers
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAtUtc)
            .Take(5)
            .Select(user => new RecentActivityResponse
            {
                ActivityType = "User",
                Description = $"{user.UserName} registered",
                OccurredAtUtc = user.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
        var recentItems = await dbContext.CollectionItems
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(5)
            .Select(item => new RecentActivityResponse
            {
                ActivityType = "Collection item",
                Description = $"{item.UserName} added {item.ItemName}",
                OccurredAtUtc = item.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new AnalyticsDashboardResponse
        {
            RangeDays = days,
            GeneratedAtUtc = DateTime.UtcNow,
            TotalUsers = totalUsers,
            TotalCollectionItems = totalItems,
            TotalCategories = totalCategories,
            TotalCurrentQuantity = totalCurrentQuantity,
            NewUsers = userDates.Count,
            NewCollectionItems = itemDates.Count,
            LowStockItems = await dbContext.CollectionItems.CountAsync(
                item => item.CurrentQuantity > 0 && item.CurrentQuantity <= 2,
                cancellationToken),
            DepletedItems = await dbContext.CollectionItems.CountAsync(
                item => item.CurrentQuantity == 0,
                cancellationToken),
            InventoryRetentionPercentage = totalStartingQuantity == 0
                ? 0
                : Math.Round(totalCurrentQuantity * 100m / totalStartingQuantity, 1),
            Trends = trends,
            PopularCategories = categories,
            TopCollectors = topCollectors,
            RecentActivity = recentUsers.Concat(recentItems)
                .OrderByDescending(activity => activity.OccurredAtUtc)
                .Take(8)
                .ToList()
        };
    }
}
