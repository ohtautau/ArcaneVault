// Name:
// Student Admin No.:
// Tutorial Group:

using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Models.Requests;

public class CreateTradeRequest
{
    [Range(1, int.MaxValue)]
    public int WishlistItemId { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Item offered in exchange")]
    public int OfferedCollectionItemId { get; set; }

    [StringLength(500)]
    public string? Message { get; set; }
}
