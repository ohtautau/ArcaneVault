// Name:
// Student Admin No.:
// Tutorial Group:

using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Models.Requests;

public class CreateCollectionItemRequest
{
    [StringLength(50)]
    [Display(Name = "Item Type ID")]
    public string? ItemTypeId { get; set; }

    public bool IsNewItemType { get; set; }
    [Required(ErrorMessage = "Item name is required.")]
    [StringLength(150, MinimumLength = 2,
        ErrorMessage = "Item name must be between 2 and 150 characters.")]
    [Display(Name = "Item Name")]
    public string ItemName { get; set; } = string.Empty;

    [Required, RegularExpression("^(Mint|Good|Fair|Poor)$")]
    public string Condition { get; set; } = "Good";

    [Required, RegularExpression("^(Common|Rare|Ultra Rare)$")]
    public string Rarity { get; set; } = "Common";

    [Range(0, int.MaxValue,
        ErrorMessage = "Starting quantity cannot be negative.")]
    [Display(Name = "Starting Quantity")]
    public int StartingQuantity { get; set; }

    [Required(ErrorMessage = "Select at least one category.")]
    [MinLength(1, ErrorMessage = "Select at least one category.")]
    [Display(Name = "Categories")]
    public List<string> CategoryCodes { get; set; } = [];
}
