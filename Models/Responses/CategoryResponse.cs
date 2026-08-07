// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Responses;

public class CategoryResponse
{
    public string CategoryCode { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public int CollectionItemCount { get; init; }
}
