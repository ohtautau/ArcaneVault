// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class Trade
{
    public int TradeId { get; set; }
    public string RequesterUserName { get; set; } = string.Empty;
    public string RecipientUserName { get; set; } = string.Empty;
    public int? WishlistItemId { get; set; }
    public WishlistItem? WishlistItem { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "Pending";
    public bool RequestedItemsLocked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? StaffResolutionNote { get; set; }
    public string? ResolvedByStaffUserName { get; set; }
    public bool RequesterConfirmedComplete { get; set; }
    public bool RecipientConfirmedComplete { get; set; }
    public string? DisputeReason { get; set; }
    public ICollection<TradeItem> TradeItems { get; set; } = [];
}
