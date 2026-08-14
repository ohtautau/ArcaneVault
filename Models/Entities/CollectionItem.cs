// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class CollectionItem
{
    public int ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public int StartingQuantity { get; set; }

    public int CurrentQuantity { get; set; }

    public string UserName { get; set; } = string.Empty;

    public ArcaneVaultUser User { get; set; } = null!;

    public ICollection<CollectionItemCategory> CollectionItemCategories { get; set; } = [];

    public ICollection<TradeItem> TradeItems { get; set; } = [];
}
