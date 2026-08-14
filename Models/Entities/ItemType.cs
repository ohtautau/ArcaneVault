namespace ArcaneVault.Models.Entities;

public class ItemType
{
    public string ItemTypeId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public ICollection<CollectionItem> CollectionItems { get; set; } = [];
}
