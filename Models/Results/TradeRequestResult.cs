// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Responses;

namespace ArcaneVault.Models.Results;

public enum TradeRequestStatus { Success, NotFound, Forbidden, OwnWishlist, InvalidOffer, AlreadyPending, NotPending, Conflict }
public record TradeRequestResult(TradeRequestStatus Status, TradeRequestResponse? Response = null);
