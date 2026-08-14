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
        var quantities = offeredIds.ToDictionary(id => id, id => request.OfferedQuantities.GetValueOrDefault(id))
            .Concat(requestedIds.ToDictionary(id => id, id => request.RequestedQuantities.GetValueOrDefault(id)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        if (quantities.Any(pair => pair.Value <= 0))
            return new(TradeStatus.InvalidItems, Detail: "Enter a trade quantity of at least 1 for every selected item.");
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
        var reserved = await db.TradeItems
            .Where(i => ids.Contains(i.CollectionItemId) && i.Trade.Status == "Pending")
            .GroupBy(i => i.CollectionItemId)
            .Select(group => new { ItemId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Quantity, ct);
        var inventory = offered.Concat(requested).ToDictionary(item => item.ItemId);
        var unavailable = ids.Where(id => quantities[id] + reserved.GetValueOrDefault(id) > inventory[id].CurrentQuantity).ToList();
        if (unavailable.Count > 0)
        {
            var details = unavailable.Select(id => $"{inventory[id].ItemName}: {inventory[id].CurrentQuantity} in stock, {reserved.GetValueOrDefault(id)} reserved");
            return new(TradeStatus.ItemsInActiveTrade, Detail: $"Not enough available quantity. {string.Join("; ", details)}.");
        }
        var trade = new Trade { RequesterUserName = user, RecipientUserName = recipients[0], WishlistItemId = request.WishlistItemId, Message = Clean(request.Message),
            TradeItems = offeredIds.Select(id => new TradeItem { CollectionItemId = id, Quantity = quantities[id], Side = "Offered" }).Concat(requestedIds.Select(id => new TradeItem { CollectionItemId = id, Quantity = quantities[id], Side = "Requested" })).ToList() };
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
        if (status == "Accepted")
        {
            if (!OriginalOwners(t) || t.TradeItems.Any(item => item.CollectionItem.CurrentQuantity < item.Quantity))
                return new(TradeStatus.Conflict, Detail: "An item owner or available quantity changed before acceptance.");
            TransferQuantities(t);
        }
        t.Status = status; t.UpdatedAtUtc = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return new(TradeStatus.Conflict); }
        return new(TradeStatus.Success, Map(t));
    }
    public async Task<TradeResult> StaffCancelAsync(int id, string staff, string note, CancellationToken ct = default)
    {
        var t = await Query(true).SingleOrDefaultAsync(x => x.TradeId == id, ct);
        if (t is null) return new(TradeStatus.NotFound);
        if (t.Status is not ("Pending" or "Accepted")) return new(TradeStatus.NotPending, Detail: "Only pending or accepted trades can be cancelled.");
        if (t.Status == "Accepted")
        {
            if (!CanReverse(t)) return new(TradeStatus.CannotReverse, Detail: "Transferred quantities changed again, so Staff cannot safely reverse this trade.");
            ReverseTransfer(t);
        }
        t.Status = "Cancelled"; t.UpdatedAtUtc = DateTime.UtcNow; t.StaffResolutionNote = note.Trim(); t.ResolvedByStaffUserName = staff;
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return new(TradeStatus.Conflict); }
        return new(TradeStatus.Success, Map(t));
    }
    private IQueryable<Trade> Query(bool tracking = false)
    {
        var q = db.Trades
            .Include(t => t.TradeItems).ThenInclude(i => i.CollectionItem).ThenInclude(item => item.CollectionItemCategories)
            .Include(t => t.TradeItems).ThenInclude(i => i.TransferredCollectionItem)
            .AsQueryable();
        return tracking ? q : q.AsNoTracking();
    }
    private static bool OriginalOwners(Trade t) => t.TradeItems.All(i => i.CollectionItem.UserName == (i.Side == "Offered" ? t.RequesterUserName : t.RecipientUserName));
    private static bool CanReverse(Trade t) =>
        t.TradeItems.All(i => i.TransferredCollectionItem is not null
            && i.TransferredCollectionItem.UserName == (i.Side == "Offered" ? t.RecipientUserName : t.RequesterUserName)
            && i.TransferredCollectionItem.CurrentQuantity >= i.Quantity)
        || t.TradeItems.All(i => i.TransferredCollectionItem is null
            && i.CollectionItem.UserName == (i.Side == "Offered" ? t.RecipientUserName : t.RequesterUserName));
    private static void TransferQuantities(Trade t)
    {
        var now = DateTime.UtcNow;
        foreach (var tradeItem in t.TradeItems)
        {
            var source = tradeItem.CollectionItem;
            source.CurrentQuantity -= tradeItem.Quantity;
            source.QuantityHistory.Add(new CollectionItemQuantityHistory { Quantity = source.CurrentQuantity, ChangedAtUtc = now });
            var destination = new CollectionItem
            {
                ItemName = source.ItemName,
                StartingQuantity = tradeItem.Quantity,
                CurrentQuantity = tradeItem.Quantity,
                UserName = tradeItem.Side == "Offered" ? t.RecipientUserName : t.RequesterUserName,
                CollectionItemCategories = source.CollectionItemCategories.Select(category => new CollectionItemCategory { CategoryCode = category.CategoryCode }).ToList()
            };
            destination.QuantityHistory.Add(new CollectionItemQuantityHistory { Quantity = tradeItem.Quantity, ChangedAtUtc = now });
            tradeItem.TransferredCollectionItem = destination;
        }
    }
    private static void ReverseTransfer(Trade t)
    {
        if (t.TradeItems.All(item => item.TransferredCollectionItem is null))
        {
            foreach (var legacyItem in t.TradeItems)
                legacyItem.CollectionItem.UserName = legacyItem.Side == "Offered" ? t.RequesterUserName : t.RecipientUserName;
            return;
        }
        var now = DateTime.UtcNow;
        foreach (var tradeItem in t.TradeItems)
        {
            var destination = tradeItem.TransferredCollectionItem!;
            destination.CurrentQuantity -= tradeItem.Quantity;
            destination.QuantityHistory.Add(new CollectionItemQuantityHistory { Quantity = destination.CurrentQuantity, ChangedAtUtc = now });
            tradeItem.CollectionItem.CurrentQuantity += tradeItem.Quantity;
            tradeItem.CollectionItem.QuantityHistory.Add(new CollectionItemQuantityHistory { Quantity = tradeItem.CollectionItem.CurrentQuantity, ChangedAtUtc = now });
        }
    }
    private static TradeResponse Map(Trade t) => new() { TradeId=t.TradeId,RequesterUserName=t.RequesterUserName,RecipientUserName=t.RecipientUserName,WishlistItemId=t.WishlistItemId,Message=t.Message,Status=t.Status,CreatedAtUtc=t.CreatedAtUtc,UpdatedAtUtc=t.UpdatedAtUtc,StaffResolutionNote=t.StaffResolutionNote,ResolvedByStaffUserName=t.ResolvedByStaffUserName,OfferedItems=t.TradeItems.Where(i=>i.Side=="Offered").Select(i=>MapItem(i,t.RequesterUserName)).ToList(),RequestedItems=t.TradeItems.Where(i=>i.Side=="Requested").Select(i=>MapItem(i,t.RecipientUserName)).ToList() };
    private static TradeItemResponse MapItem(TradeItem i,string owner)=>new(){CollectionItemId=i.CollectionItemId,ItemName=i.CollectionItem.ItemName,OriginalOwnerUserName=owner,CurrentQuantity=i.CollectionItem.CurrentQuantity,TradeQuantity=i.Quantity};
    private static string? Clean(string? s)=>string.IsNullOrWhiteSpace(s)?null:s.Trim();
}
