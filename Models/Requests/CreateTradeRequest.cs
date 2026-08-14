using System.ComponentModel.DataAnnotations;
namespace ArcaneVault.Models.Requests;
public class CreateTradeRequest
{
    [Required, StringLength(50)] public string RecipientUserName { get; set; } = string.Empty;
    [Required, MinLength(1)] public List<int> OfferedCollectionItemIds { get; set; } = [];
    [Required, MinLength(1)] public List<int> RequestedCollectionItemIds { get; set; } = [];
    public Dictionary<int, int> OfferedQuantities { get; set; } = [];
    public Dictionary<int, int> RequestedQuantities { get; set; } = [];
    public int? WishlistItemId { get; set; }
    [StringLength(500)] public string? Message { get; set; }
}
public class UpdateTradeStatusRequest
{
    [Required, RegularExpression("^(Accepted|Rejected|Cancelled|Confirmed|Disputed)$")] public string Status { get; set; } = string.Empty;
    [StringLength(500)] public string? DisputeReason { get; set; }
}
public class StaffResolveTradeRequest
{
    [Required, RegularExpression("^(Completed|Cancelled)$")] public string Resolution { get; set; } = string.Empty;
    [Required, StringLength(500, MinimumLength = 5)] public string ResolutionNote { get; set; } = string.Empty;
}
public class StaffCancelTradeRequest
{
    [Required, StringLength(500, MinimumLength = 5)] public string ResolutionNote { get; set; } = string.Empty;
}
