// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Responses;

public class CollectionItemResponse
{
    public int ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int StartingQuantity { get; set; }

    public int CurrentQuantity { get; set; }

    public string UserName { get; set; } = string.Empty;

    public IReadOnlyList<CollectionItemCategoryResponse> Categories { get; set; } = [];
}
