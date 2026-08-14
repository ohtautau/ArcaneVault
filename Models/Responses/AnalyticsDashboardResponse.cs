// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Responses;

public class AnalyticsDashboardResponse
{
    public int RangeDays { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public int TotalUsers { get; set; }
    public int TotalCollectionItems { get; set; }
    public int TotalCategories { get; set; }
    public int TotalCurrentQuantity { get; set; }
    public int NewUsers { get; set; }
    public int NewCollectionItems { get; set; }
    public int LowStockItems { get; set; }
    public int DepletedItems { get; set; }
    public decimal InventoryRetentionPercentage { get; set; }
    public IReadOnlyList<AnalyticsTrendPointResponse> Trends { get; set; } = [];
    public IReadOnlyList<PopularCategoryResponse> PopularCategories { get; set; } = [];
    public IReadOnlyList<TopCollectorResponse> TopCollectors { get; set; } = [];
    public IReadOnlyList<UserInventoryResponse> UserInventory { get; set; } = [];
    public IReadOnlyList<MostTradedItemResponse> MostTradedItems { get; set; } = [];
    public IReadOnlyList<ActiveUserResponse> MostActiveUsers { get; set; } = [];
    public IReadOnlyList<RecentActivityResponse> RecentActivity { get; set; } = [];
}

public class UserInventoryResponse
{
    public string UserName { get; set; } = string.Empty;
    public int CollectionItemCount { get; set; }
    public int CurrentQuantity { get; set; }
}

public class MostTradedItemResponse
{
    public string ItemTypeId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int TradeCount { get; set; }
    public int QuantityTraded { get; set; }
}

public class ActiveUserResponse
{
    public string UserName { get; set; } = string.Empty;
    public int Trades { get; set; }
    public int InventoryChanges { get; set; }
    public int ActivityScore { get; set; }
}

public class AnalyticsTrendPointResponse
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
    public int NewCollectionItems { get; set; }
}

public class PopularCategoryResponse
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CollectionItemCount { get; set; }
    public decimal Percentage { get; set; }
}

public class TopCollectorResponse
{
    public string UserName { get; set; } = string.Empty;
    public int CollectionItemCount { get; set; }
    public int CurrentQuantity { get; set; }
}

public class RecentActivityResponse
{
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
}
