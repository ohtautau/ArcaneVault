// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Data;
using ArcaneVault.Models.Entities;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.Services;

public class TradeRequestService(ArcaneVaultDbContext dbContext) : ITradeRequestService
{
    public async Task<IReadOnlyList<TradeRequestResponse>> GetForUserAsync(string userName, CancellationToken cancellationToken = default) =>
        await Project(dbContext.TradeRequests.AsNoTracking().Where(request => request.OwnerUserName == userName || request.RequesterUserName == userName))
            .OrderBy(request => request.Status == "Pending" ? 0 : 1).ThenByDescending(request => request.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<TradeRequestResult> CreateAsync(string userName, CreateTradeRequest request, CancellationToken cancellationToken = default)
    {
        var wishlist = await dbContext.WishlistItems.AsNoTracking().SingleOrDefaultAsync(item => item.WishlistItemId == request.WishlistItemId, cancellationToken);
        if (wishlist is null) return new(TradeRequestStatus.NotFound);
        if (string.Equals(wishlist.UserName, userName, StringComparison.OrdinalIgnoreCase)) return new(TradeRequestStatus.OwnWishlist);
        var validOffer = await dbContext.CollectionItems.AnyAsync(item => item.ItemId == request.OfferedCollectionItemId && item.UserName == userName, cancellationToken);
        if (!validOffer) return new(TradeRequestStatus.InvalidOffer);
        var duplicate = await dbContext.TradeRequests.AnyAsync(existing => existing.WishlistItemId == request.WishlistItemId && existing.RequesterUserName == userName && existing.Status == "Pending", cancellationToken);
        if (duplicate) return new(TradeRequestStatus.AlreadyPending);
        var trade = new TradeRequest { WishlistItemId = wishlist.WishlistItemId, OfferedCollectionItemId = request.OfferedCollectionItemId, RequesterUserName = userName, OwnerUserName = wishlist.UserName, Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim() };
        dbContext.TradeRequests.Add(trade);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return new(TradeRequestStatus.Conflict); }
        return new(TradeRequestStatus.Success, await Project(dbContext.TradeRequests.AsNoTracking().Where(item => item.TradeRequestId == trade.TradeRequestId)).SingleAsync(cancellationToken));
    }

    public async Task<TradeRequestResult> UpdateStatusAsync(int id, string userName, string status, CancellationToken cancellationToken = default)
    {
        var trade = await dbContext.TradeRequests.SingleOrDefaultAsync(request => request.TradeRequestId == id, cancellationToken);
        if (trade is null) return new(TradeRequestStatus.NotFound);
        if (trade.Status != "Pending") return new(TradeRequestStatus.NotPending);
        var isOwner = string.Equals(trade.OwnerUserName, userName, StringComparison.OrdinalIgnoreCase);
        var isRequester = string.Equals(trade.RequesterUserName, userName, StringComparison.OrdinalIgnoreCase);
        if ((status is "Accepted" or "Declined") && !isOwner || status == "Cancelled" && !isRequester) return new(TradeRequestStatus.Forbidden);
        trade.Status = status; trade.RespondedAtUtc = DateTime.UtcNow;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return new(TradeRequestStatus.Conflict); }
        return new(TradeRequestStatus.Success, await Project(dbContext.TradeRequests.AsNoTracking().Where(item => item.TradeRequestId == id)).SingleAsync(cancellationToken));
    }

    private static IQueryable<TradeRequestResponse> Project(IQueryable<TradeRequest> query) => query.Select(request => new TradeRequestResponse
    {
        TradeRequestId = request.TradeRequestId, WishlistItemId = request.WishlistItemId, WantedItemName = request.WishlistItem.ItemName,
        OfferedCollectionItemId = request.OfferedCollectionItemId, OfferedItemName = request.OfferedCollectionItem.ItemName,
        RequesterUserName = request.RequesterUserName, OwnerUserName = request.OwnerUserName, Message = request.Message,
        Status = request.Status, CreatedAtUtc = request.CreatedAtUtc, RespondedAtUtc = request.RespondedAtUtc
    });
}
