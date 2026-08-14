namespace ArcaneVault.Models.Responses;
public class TradeResponse
{
    public int TradeId { get; set; }
    public string RequesterUserName { get; set; } = string.Empty;
    public string RecipientUserName { get; set; } = string.Empty;
    public int? WishlistItemId { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? StaffResolutionNote { get; set; }
    public string? ResolvedByStaffUserName { get; set; }
    public bool RequesterConfirmedComplete { get; set; }
    public bool RecipientConfirmedComplete { get; set; }
    public string? DisputeReason { get; set; }
    public IReadOnlyList<TradeItemResponse> OfferedItems { get; set; } = [];
    public IReadOnlyList<TradeItemResponse> RequestedItems { get; set; } = [];
}
public class TradeItemResponse
{
    public int CollectionItemId { get; set; }
    public string ItemTypeId { get; set; } = string.Empty;
    public int? ReceivedCollectionItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Condition { get; set; } = "Good";
    public string Rarity { get; set; } = "Common";
    public string? ImagePath { get; set; }
    public string OriginalOwnerUserName { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int TradeQuantity { get; set; }
    public int LockedQuantity { get; set; }
    public bool IsInTrade { get; set; }
}
public class TradePartnerResponse
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
