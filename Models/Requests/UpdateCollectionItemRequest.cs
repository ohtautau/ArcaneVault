// Name:
// Student Admin No.:
// Tutorial Group:

using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Models.Requests;

public class UpdateCollectionItemRequest
{
    [Required(ErrorMessage = "Item name is required.")]
    [StringLength(150, MinimumLength = 2,
        ErrorMessage = "Item name must be between 2 and 150 characters.")]
    [Display(Name = "Item Name")]
    public string ItemName { get; set; } = string.Empty;

    [Range(0, int.MaxValue,
        ErrorMessage = "Current quantity cannot be negative.")]
    [Display(Name = "Current Quantity")]
    public int CurrentQuantity { get; set; }

    [Required(ErrorMessage = "Select at least one category.")]
    [MinLength(1, ErrorMessage = "Select at least one category.")]
    [Display(Name = "Categories")]
    public List<string> CategoryCodes { get; set; } = [];
}
