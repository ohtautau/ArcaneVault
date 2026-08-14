// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class TradeItem
{
    public int TradeItemId { get; set; }
    public int TradeId { get; set; }
    public Trade Trade { get; set; } = null!;
    public int CollectionItemId { get; set; }
    public CollectionItem CollectionItem { get; set; } = null!;
    public string Side { get; set; } = string.Empty;
}
