// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Data;
using ArcaneVault.Models.Responses;
using Microsoft.EntityFrameworkCore;
using System.Text;

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

        var userInventory = await dbContext.ArcaneVaultUsers
            .AsNoTracking()
            .Where(user => user.Role.RoleName == "User")
            .Select(user => new UserInventoryResponse
            {
                UserName = user.UserName,
                CollectionItemCount = user.CollectionItems.Count,
                CurrentQuantity = user.CollectionItems.Sum(item => (int?)item.CurrentQuantity) ?? 0
            })
            .OrderByDescending(user => user.CollectionItemCount)
            .ThenBy(user => user.UserName)
            .ToListAsync(cancellationToken);
        var topCollectors = userInventory.Take(5).Select(user => new TopCollectorResponse
        {
            UserName = user.UserName,
            CollectionItemCount = user.CollectionItemCount,
            CurrentQuantity = user.CurrentQuantity
        }).ToList();

        var completedTradeItems = await dbContext.TradeItems.AsNoTracking()
            .Where(item => item.Trade.Status == "Completed")
            .Select(item => new { item.TradeId, item.CollectionItem.ItemTypeId, item.CollectionItem.ItemName, item.Quantity })
            .ToListAsync(cancellationToken);
        var mostTradedItems = completedTradeItems
            .GroupBy(item => new { item.ItemTypeId, item.ItemName })
            .Select(group => new MostTradedItemResponse
            {
                ItemTypeId = group.Key.ItemTypeId,
                ItemName = group.Key.ItemName,
                TradeCount = group.Select(item => item.TradeId).Distinct().Count(),
                QuantityTraded = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(item => item.QuantityTraded).ThenByDescending(item => item.TradeCount).Take(10).ToList();

        var rangeTrades = await dbContext.Trades.AsNoTracking()
            .Where(trade => trade.CreatedAtUtc >= fromUtc || trade.UpdatedAtUtc >= fromUtc)
            .Select(trade => new { trade.RequesterUserName, trade.RecipientUserName })
            .ToListAsync(cancellationToken);
        var rangeChanges = await dbContext.CollectionItemQuantityHistory.AsNoTracking()
            .Where(change => change.ChangedAtUtc >= fromUtc)
            .GroupBy(change => change.CollectionItem.UserName)
            .Select(group => new { UserName = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.UserName, item => item.Count, cancellationToken);
        var mostActiveUsers = userInventory.Select(user =>
        {
            var trades = rangeTrades.Count(trade =>
                string.Equals(trade.RequesterUserName, user.UserName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(trade.RecipientUserName, user.UserName, StringComparison.OrdinalIgnoreCase));
            var changes = rangeChanges.GetValueOrDefault(user.UserName);
            return new ActiveUserResponse { UserName = user.UserName, Trades = trades, InventoryChanges = changes, ActivityScore = trades + changes };
        }).OrderByDescending(user => user.ActivityScore).ThenByDescending(user => user.Trades).ThenBy(user => user.UserName).Take(10).ToList();

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
        var recentTrades = await dbContext.Trades.AsNoTracking()
            .OrderByDescending(trade => trade.UpdatedAtUtc ?? trade.CreatedAtUtc).Take(8)
            .Select(trade => new RecentActivityResponse
            {
                ActivityType = "Trade",
                Description = $"Trade #{trade.TradeId}: {trade.RequesterUserName} ↔ {trade.RecipientUserName} — {trade.Status}",
                OccurredAtUtc = trade.UpdatedAtUtc ?? trade.CreatedAtUtc
            }).ToListAsync(cancellationToken);
        var recentQuantityChanges = await dbContext.CollectionItemQuantityHistory.AsNoTracking()
            .OrderByDescending(change => change.ChangedAtUtc).Take(8)
            .Select(change => new RecentActivityResponse
            {
                ActivityType = "Inventory",
                Description = $"{change.CollectionItem.UserName}'s {change.CollectionItem.ItemName} quantity became {change.Quantity}",
                OccurredAtUtc = change.ChangedAtUtc
            }).ToListAsync(cancellationToken);

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
            UserInventory = userInventory,
            MostTradedItems = mostTradedItems,
            MostActiveUsers = mostActiveUsers,
            RecentActivity = recentUsers.Concat(recentItems).Concat(recentTrades).Concat(recentQuantityChanges)
                .OrderByDescending(activity => activity.OccurredAtUtc)
                .Take(12)
                .ToList()
        };
    }

    public async Task<byte[]> ExportCollectionsCsvAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.CollectionItems.AsNoTracking()
            .Include(item => item.CollectionItemCategories).ThenInclude(link => link.Category)
            .OrderBy(item => item.UserName).ThenBy(item => item.ItemName)
            .ToListAsync(cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("Item Type ID,Inventory Record ID,Item Name,Owner,Starting Quantity,Current Quantity,Locked Quantity,Available Quantity,Categories,Created UTC");
        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                Csv(row.ItemTypeId), row.ItemId.ToString(), Csv(row.ItemName), Csv(row.UserName),
                row.StartingQuantity.ToString(), row.CurrentQuantity.ToString(), row.LockedQuantity.ToString(),
                (row.CurrentQuantity - row.LockedQuantity).ToString(),
                Csv(string.Join(" | ", row.CollectionItemCategories.OrderBy(link => link.CategoryCode).Select(link => link.Category.CategoryName))),
                Csv(row.CreatedAtUtc.ToString("O"))
            }));
        }
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(csv.ToString())).ToArray();
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
