// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class CollectionItemCategory
{
    public int ItemId { get; set; }

    public string CategoryCode { get; set; } = string.Empty;

    public CollectionItem CollectionItem { get; set; } = null!;

    public Category Category { get; set; } = null!;
}
