// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;

namespace ArcaneVault.Models.Results;

public enum WishlistStatus { Success, NotFound, Forbidden, HasPendingTrades, Conflict }
public record WishlistResult(WishlistStatus Status, WishlistItemResponse? Response = null);
