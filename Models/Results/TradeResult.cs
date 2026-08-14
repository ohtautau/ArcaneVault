using ArcaneVault.Models.Responses;
namespace ArcaneVault.Models.Results;
public enum TradeStatus { Success, NotFound, Forbidden, InvalidItems, SameUser, ItemsInActiveTrade, NotPending, CannotReverse, Conflict }
public record TradeResult(TradeStatus Status, TradeResponse? Response = null, string? Detail = null);
