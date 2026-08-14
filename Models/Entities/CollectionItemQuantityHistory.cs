// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class CollectionItemQuantityHistory
{
    public int HistoryId { get; set; }
    public int ItemId { get; set; }
    public CollectionItem CollectionItem { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
