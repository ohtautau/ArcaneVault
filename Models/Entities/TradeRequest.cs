// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class TradeRequest
{
    public int TradeRequestId { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; } = null!;
    public int OfferedCollectionItemId { get; set; }
    public CollectionItem OfferedCollectionItem { get; set; } = null!;
    public string RequesterUserName { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }
}
