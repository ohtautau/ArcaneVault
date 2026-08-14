// Name:
// Student Admin No.:
// Tutorial Group:

using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Models.Requests;

public class CreateWishlistItemRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    [Display(Name = "Wanted item")]
    public string ItemName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Desired quantity")]
    public int DesiredQuantity { get; set; } = 1;
}
