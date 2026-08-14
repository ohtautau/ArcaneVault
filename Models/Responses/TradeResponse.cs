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
    public IReadOnlyList<TradeItemResponse> OfferedItems { get; set; } = [];
    public IReadOnlyList<TradeItemResponse> RequestedItems { get; set; } = [];
}
public class TradeItemResponse
{
    public int CollectionItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string OriginalOwnerUserName { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int TradeQuantity { get; set; }
}
