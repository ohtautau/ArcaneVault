// Name:
// Student Admin No.:
// Tutorial Group:

using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Models.Requests;

public class CreateCategoryRequest
{
    [Required(ErrorMessage = "Category code is required.")]
    [StringLength(20, MinimumLength = 2,
        ErrorMessage = "Category code must be between 2 and 20 characters.")]
    [RegularExpression("^[a-zA-Z0-9_-]+$",
        ErrorMessage = "Category code can contain only letters, numbers, hyphens, and underscores.")]
    [Display(Name = "Category Code")]
    public string CategoryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Category name must be between 2 and 100 characters.")]
    [Display(Name = "Category Name")]
    public string CategoryName { get; set; } = string.Empty;
}
