// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class Category
{
    public string CategoryCode { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public ICollection<CollectionItemCategory> CollectionItemCategories { get; set; } = [];
}
