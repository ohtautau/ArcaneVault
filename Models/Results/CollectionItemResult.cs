// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;

namespace ArcaneVault.Models.Results;

public enum CollectionItemStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidCategories,
    InvalidItemType,
    InvalidImage,
    Conflict
}

public record CollectionItemResult(
    CollectionItemStatus Status,
    CollectionItemResponse? Response = null,
    IReadOnlyList<string>? InvalidCategoryCodes = null);
