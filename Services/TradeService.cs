using ArcaneVault.Data;
using ArcaneVault.Models.Entities;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using Microsoft.EntityFrameworkCore;
namespace ArcaneVault.Services;
public class TradeService(ArcaneVaultDbContext db) : ITradeService
{
    public async Task<IReadOnlyList<TradeResponse>> GetForUserAsync(string user, CancellationToken ct = default) =>
        (await Query().Where(t => t.RequesterUserName == user || t.RecipientUserName == user).OrderByDescending(t => t.CreatedAtUtc).ToListAsync(ct)).Select(Map).ToList();
    public async Task<IReadOnlyList<TradeResponse>> GetAllActiveAsync(CancellationToken ct = default) =>
        (await Query().Where(t => t.Status == "Pending" || t.Status == "Accepted").OrderByDescending(t => t.CreatedAtUtc).ToListAsync(ct)).Select(Map).ToList();
    public async Task<TradeResult> GetByIdAsync(int id, string user, bool staff, CancellationToken ct = default)
    {
        var t = await Query().SingleOrDefaultAsync(x => x.TradeId == id, ct);
        if (t is null) return new(TradeStatus.NotFound);
        if (!staff && t.RequesterUserName != user && t.RecipientUserName != user) return new(TradeStatus.Forbidden);
        return new(TradeStatus.Success, Map(t));
    }
    public async Task<TradeResult> CreateAsync(string user, CreateTradeRequest request, CancellationToken ct = default)
    {
        var offeredIds = request.OfferedCollectionItemIds.Distinct().ToList();
        var requestedIds = request.RequestedCollectionItemIds.Distinct().ToList();
        if (offeredIds.Count == 0 || requestedIds.Count == 0 || offeredIds.Intersect(requestedIds).Any()) return new(TradeStatus.InvalidItems, Detail: "Both sides need different collection items.");
        var offered = await db.CollectionItems.Where(i => offeredIds.Contains(i.ItemId)).ToListAsync(ct);
        var requested = await db.CollectionItems.Where(i => requestedIds.Contains(i.ItemId)).ToListAsync(ct);
        if (offered.Count != offeredIds.Count || offered.Any(i => i.UserName != user) || requested.Count != requestedIds.Count) return new(TradeStatus.InvalidItems, Detail: "Offered items must belong to you and requested items must exist.");
        var recipients = requested.Select(i => i.UserName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (recipients.Count != 1 || string.Equals(recipients[0], user, StringComparison.OrdinalIgnoreCase)) return new(TradeStatus.SameUser, Detail: "Requested items must belong to one other user.");
        if (request.WishlistItemId is int wishlistItemId
            && !await db.WishlistItems.AnyAsync(
                item => item.WishlistItemId == wishlistItemId
                    && item.UserName == recipients[0],
                ct))
        {
            return new(TradeStatus.InvalidItems, Detail: "The selected wishlist entry does not belong to the requested-item owner.");
        }
        var ids = offeredIds.Concat(requestedIds).ToList();
        if (await db.TradeItems.AnyAsync(i => ids.Contains(i.CollectionItemId) && (i.Trade.Status == "Pending" || i.Trade.Status == "Accepted"), ct)) return new(TradeStatus.ItemsInActiveTrade, Detail: "An item already belongs to an active trade.");
        var trade = new Trade { RequesterUserName = user, RecipientUserName = recipients[0], WishlistItemId = request.WishlistItemId, Message = Clean(request.Message),
            TradeItems = offeredIds.Select(id => new TradeItem { CollectionItemId = id, Side = "Offered" }).Concat(requestedIds.Select(id => new TradeItem { CollectionItemId = id, Side = "Requested" })).ToList() };
        db.Trades.Add(trade);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return new(TradeStatus.Conflict); }
        return await GetByIdAsync(trade.TradeId, user, false, ct);
    }
    public async Task<TradeResult> UpdateStatusAsync(int id, string user, string status, CancellationToken ct = default)
    {
        var t = await Query(true).SingleOrDefaultAsync(x => x.TradeId == id, ct);
        if (t is null) return new(TradeStatus.NotFound);
        if (t.Status != "Pending") return new(TradeStatus.NotPending, Detail: "Only pending trades can be processed.");
        var recipient = string.Equals(t.RecipientUserName, user, StringComparison.OrdinalIgnoreCase);
        var requester = string.Equals(t.RequesterUserName, user, StringComparison.OrdinalIgnoreCase);
        if ((status is "Accepted" or "Rejected") && !recipient || status == "Cancelled" && !requester) return new(TradeStatus.Forbidden);
        if (status == "Accepted") { if (!OriginalOwners(t)) return new(TradeStatus.Conflict, Detail: "Item ownership changed before acceptance."); Transfer(t, false); }
        t.Status = status; t.UpdatedAtUtc = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return new(TradeStatus.Conflict); }
        return new(TradeStatus.Success, Map(t));
    }
    public async Task<TradeResult> StaffCancelAsync(int id, string staff, string note, CancellationToken ct = default)
    {
        var t = await Query(true).SingleOrDefaultAsync(x => x.TradeId == id, ct);
        if (t is null) return new(TradeStatus.NotFound);
        if (t.Status is not ("Pending" or "Accepted")) return new(TradeStatus.NotPending, Detail: "Only pending or accepted trades can be cancelled.");
        if (t.Status == "Accepted") { if (!TransferredOwners(t)) return new(TradeStatus.CannotReverse, Detail: "Ownership changed again, so Staff cannot safely reverse this trade."); Transfer(t, true); }
        t.Status = "Cancelled"; t.UpdatedAtUtc = DateTime.UtcNow; t.StaffResolutionNote = note.Trim(); t.ResolvedByStaffUserName = staff;
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return new(TradeStatus.Conflict); }
        return new(TradeStatus.Success, Map(t));
    }
    private IQueryable<Trade> Query(bool tracking = false) { var q = db.Trades.Include(t => t.TradeItems).ThenInclude(i => i.CollectionItem).AsQueryable(); return tracking ? q : q.AsNoTracking(); }
    private static bool OriginalOwners(Trade t) => t.TradeItems.All(i => i.CollectionItem.UserName == (i.Side == "Offered" ? t.RequesterUserName : t.RecipientUserName));
    private static bool TransferredOwners(Trade t) => t.TradeItems.All(i => i.CollectionItem.UserName == (i.Side == "Offered" ? t.RecipientUserName : t.RequesterUserName));
    private static void Transfer(Trade t, bool reverse) { foreach (var i in t.TradeItems) i.CollectionItem.UserName = (i.Side == "Offered") ^ reverse ? t.RecipientUserName : t.RequesterUserName; }
    private static TradeResponse Map(Trade t) => new() { TradeId=t.TradeId,RequesterUserName=t.RequesterUserName,RecipientUserName=t.RecipientUserName,WishlistItemId=t.WishlistItemId,Message=t.Message,Status=t.Status,CreatedAtUtc=t.CreatedAtUtc,UpdatedAtUtc=t.UpdatedAtUtc,StaffResolutionNote=t.StaffResolutionNote,ResolvedByStaffUserName=t.ResolvedByStaffUserName,OfferedItems=t.TradeItems.Where(i=>i.Side=="Offered").Select(i=>MapItem(i,t.RequesterUserName)).ToList(),RequestedItems=t.TradeItems.Where(i=>i.Side=="Requested").Select(i=>MapItem(i,t.RecipientUserName)).ToList() };
    private static TradeItemResponse MapItem(TradeItem i,string owner)=>new(){CollectionItemId=i.CollectionItemId,ItemName=i.CollectionItem.ItemName,OriginalOwnerUserName=owner,CurrentQuantity=i.CollectionItem.CurrentQuantity};
    private static string? Clean(string? s)=>string.IsNullOrWhiteSpace(s)?null:s.Trim();
}
