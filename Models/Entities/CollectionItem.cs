// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class CollectionItem
{
    public int ItemId { get; set; }

    public string ItemTypeId { get; set; } = string.Empty;

    public ItemType ItemType { get; set; } = null!;

    public string ItemName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int StartingQuantity { get; set; }

    public int CurrentQuantity { get; set; }

    public int LockedQuantity { get; set; }

    public bool IsInTrade { get; set; }

    public string UserName { get; set; } = string.Empty;

    public ArcaneVaultUser User { get; set; } = null!;

    public ICollection<CollectionItemCategory> CollectionItemCategories { get; set; } = [];

    public ICollection<TradeItem> TradeItems { get; set; } = [];

    public ICollection<TradeItem> ReceivedTradeItems { get; set; } = [];

    public ICollection<CollectionItemQuantityHistory> QuantityHistory { get; set; } = [];
}
