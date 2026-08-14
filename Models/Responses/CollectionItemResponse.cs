// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Responses;

public class CollectionItemResponse
{
    public int ItemId { get; set; }

    public string ItemTypeId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public string Condition { get; set; } = string.Empty;

    public string Rarity { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public int StartingQuantity { get; set; }

    public int CurrentQuantity { get; set; }

    public int LockedQuantity { get; set; }

    public bool IsInTrade { get; set; }

    public int RecentQuantityChange { get; set; }

    public int ChangePeriodDays { get; set; }

    public string UserName { get; set; } = string.Empty;

    public IReadOnlyList<CollectionItemCategoryResponse> Categories { get; set; } = [];

    public IReadOnlyList<QuantityHistoryPointResponse> QuantityHistory { get; set; } = [];
}

public class ItemTypeResponse
{
    public string ItemTypeId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
}

public class QuantityHistoryPointResponse
{
    public int Quantity { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
