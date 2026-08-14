// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Responses;

public class WishlistItemResponse
{
    public int WishlistItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int DesiredQuantity { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int PendingTradeCount { get; set; }
}
