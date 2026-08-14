// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Responses;

public class TradeRequestResponse
{
    public int TradeRequestId { get; set; }
    public int WishlistItemId { get; set; }
    public string WantedItemName { get; set; } = string.Empty;
    public int OfferedCollectionItemId { get; set; }
    public string OfferedItemName { get; set; } = string.Empty;
    public string RequesterUserName { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
}
