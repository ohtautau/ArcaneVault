// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;

namespace ArcaneVault.Models.Results;

public enum CategoryStatus
{
    Success,
    NotFound,
    DuplicateCode,
    DuplicateName,
    InUse,
    Conflict
}

public record CategoryResult(
    CategoryStatus Status,
    CategoryResponse? Response = null);
