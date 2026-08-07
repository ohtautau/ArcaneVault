// Name:
// Student Admin No.:
// Tutorial Group:

using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Models.Requests;

public class UpdateCategoryRequest
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Category name must be between 2 and 100 characters.")]
    [Display(Name = "Category Name")]
    public string CategoryName { get; set; } = string.Empty;
}
