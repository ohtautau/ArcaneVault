using System.ComponentModel.DataAnnotations;
namespace ArcaneVault.Models.Requests;
public class CreateTradeRequest
{
    [Required, MinLength(1)] public List<int> OfferedCollectionItemIds { get; set; } = [];
    [Required, MinLength(1)] public List<int> RequestedCollectionItemIds { get; set; } = [];
    public int? WishlistItemId { get; set; }
    [StringLength(500)] public string? Message { get; set; }
}
public class UpdateTradeStatusRequest
{
    [Required, RegularExpression("^(Accepted|Rejected|Cancelled)$")] public string Status { get; set; } = string.Empty;
}
public class StaffCancelTradeRequest
{
    [Required, StringLength(500, MinimumLength = 5)] public string ResolutionNote { get; set; } = string.Empty;
}
