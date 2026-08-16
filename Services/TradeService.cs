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
        (await Query().Where(t => t.Status == "Pending" || t.Status == "Trading" || t.Status == "Disputed").OrderByDescending(t => t.CreatedAtUtc).ToListAsync(ct)).Select(Map).ToList();
    public async Task<IReadOnlyList<TradeResponse>> GetAllForStaffAsync(CancellationToken ct = default) =>
        (await Query().OrderByDescending(t => t.CreatedAtUtc).ToListAsync(ct)).Select(Map).ToList();
    public async Task<IReadOnlyList<TradePartnerResponse>> GetPartnersAsync(string user, CancellationToken ct = default) =>
        await db.ArcaneVaultUsers.AsNoTracking().Where(x => x.UserName != user && x.Role.RoleName == "User")
            .OrderBy(x => x.UserName).Select(x => new TradePartnerResponse { UserName = x.UserName, Email = x.Email }).ToListAsync(ct);
    public async Task<TradeResult> GetByIdAsync(int id, string user, bool staff, CancellationToken ct = default)
    {
        var t = await Query().SingleOrDefaultAsync(x => x.TradeId == id, ct);
        if (t is null) return new(TradeStatus.NotFound);
        if (!staff && t.RequesterUserName != user && t.RecipientUserName != user) return new(TradeStatus.Forbidden);
        return new(TradeStatus.Success, Map(t));
    }
    public async Task<TradeResult> CreateAsync(string user, CreateTradeRequest request, CancellationToken ct = default)
    {
        var recipientName = request.RecipientUserName.Trim();
        if (string.Equals(recipientName, user, StringComparison.OrdinalIgnoreCase)) return new(TradeStatus.SameUser);
        if (!await db.ArcaneVaultUsers.AnyAsync(x => x.UserName == recipientName, ct)) return new(TradeStatus.InvalidItems, Detail: "The selected recipient does not exist.");
        var offeredIds = request.OfferedCollectionItemIds.Distinct().ToList();
        var requestedIds = request.RequestedCollectionItemIds.Distinct().ToList();
        if (offeredIds.Count == 0 || requestedIds.Count == 0 || offeredIds.Intersect(requestedIds).Any()) return new(TradeStatus.InvalidItems, Detail: "Both sides need different collection items.");
        var offered = await db.CollectionItems.Where(i => offeredIds.Contains(i.ItemId)).ToListAsync(ct);
        var requested = await db.CollectionItems.Where(i => requestedIds.Contains(i.ItemId)).ToListAsync(ct);
        if (offered.Count != offeredIds.Count || offered.Any(i => i.UserName != user) || requested.Count != requestedIds.Count || requested.Any(i => i.UserName != recipientName)) return new(TradeStatus.InvalidItems, Detail: "Offered items must belong to you and requested items must belong to the selected recipient.");
        var quantities = offeredIds.ToDictionary(id => id, id => request.OfferedQuantities.GetValueOrDefault(id))
            .Concat(requestedIds.ToDictionary(id => id, id => request.RequestedQuantities.GetValueOrDefault(id)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        if (quantities.Any(pair => pair.Value <= 0))
            return new(TradeStatus.InvalidItems, Detail: "Enter a trade quantity of at least 1 for every selected item.");
        if (request.WishlistItemId is int wishlistItemId
            && !await db.WishlistItems.AnyAsync(
                item => item.WishlistItemId == wishlistItemId
                    && item.UserName == recipientName,
                ct))
        {
            return new(TradeStatus.InvalidItems, Detail: "The selected wishlist entry does not belong to the requested-item owner.");
        }
        var ids = offeredIds.Concat(requestedIds).ToList();
        var inventory = offered.Concat(requested).ToDictionary(item => item.ItemId);
        var unavailable = ids.Where(id => quantities[id] > inventory[id].CurrentQuantity - inventory[id].LockedQuantity).ToList();
        if (unavailable.Count > 0)
        {
            var details = unavailable.Select(id => $"{inventory[id].ItemName}: {inventory[id].CurrentQuantity - inventory[id].LockedQuantity} available");
            return new(TradeStatus.ItemsInActiveTrade, Detail: $"Not enough available quantity. {string.Join("; ", details)}.");
        }
        var trade = new Trade { RequesterUserName = user, RecipientUserName = recipientName, WishlistItemId = request.WishlistItemId, Message = Clean(request.Message),
            TradeItems = offeredIds.Select(id => new TradeItem { CollectionItemId = id, Quantity = quantities[id], Side = "Offered" }).Concat(requestedIds.Select(id => new TradeItem { CollectionItemId = id, Quantity = quantities[id], Side = "Requested" })).ToList() };
        foreach (var item in requested)
        {
            item.LockedQuantity += quantities[item.ItemId];
            item.IsInTrade = true;
        }
        trade.RequestedItemsLocked = true;
        db.Trades.Add(trade);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return new(TradeStatus.Conflict); }
        return await GetByIdAsync(trade.TradeId, user, false, ct);
    }
    public Task<TradeResult> UpdateStatusAsync(int id, string user, string status, CancellationToken ct = default) => UpdateStatusAsync(id, user, status, null, ct);
    public async Task<TradeResult> UpdateStatusAsync(int id, string user, string status, string? disputeReason, CancellationToken ct = default)
    {
        var t = await Query(true).SingleOrDefaultAsync(x => x.TradeId == id, ct);
        if (t is null) return new(TradeStatus.NotFound);
        var recipient = string.Equals(t.RecipientUserName, user, StringComparison.OrdinalIgnoreCase);
        var requester = string.Equals(t.RequesterUserName, user, StringComparison.OrdinalIgnoreCase);
        if (!recipient && !requester) return new(TradeStatus.Forbidden);
        if (t.Status == "Pending")
        {
            if ((status is "Accepted" or "Rejected") && !recipient || status == "Cancelled" && !requester) return new(TradeStatus.Forbidden);
            if (status == "Accepted")
            {
                if (!OriginalOwners(t)
                    || t.TradeItems.Where(item => item.Side == "Offered")
                        .Any(item => item.CollectionItem.CurrentQuantity - item.CollectionItem.LockedQuantity < item.Quantity)
                    || t.TradeItems.Where(item => item.Side == "Requested").Any(item =>
                        t.RequestedItemsLocked
                            ? item.CollectionItem.CurrentQuantity < item.Quantity || item.CollectionItem.LockedQuantity < item.Quantity
                            : item.CollectionItem.CurrentQuantity - item.CollectionItem.LockedQuantity < item.Quantity))
                return new(TradeStatus.Conflict, Detail: "An item owner or available quantity changed before acceptance.");
                if (!t.RequestedItemsLocked) { LockItems(t, "Requested"); t.RequestedItemsLocked = true; }
                LockItems(t, "Offered"); t.Status = "Trading";
            }
            else if (status is "Rejected" or "Cancelled") { if (t.RequestedItemsLocked) UnlockItems(t, "Requested"); t.Status = status; }
            else return new(TradeStatus.NotPending, Detail: "This action is not valid for a pending trade.");
        }
        else if (t.Status == "Trading" && status == "Confirmed")
        {
            if (requester) t.RequesterConfirmedComplete = true; else t.RecipientConfirmedComplete = true;
            if (t.RequesterConfirmedComplete && t.RecipientConfirmedComplete) { await TransferQuantitiesAsync(t, ct); UnlockItems(t); t.Status = "Completed"; }
        }
        else if (t.Status == "Trading" && status == "Disputed")
        {
            if (string.IsNullOrWhiteSpace(disputeReason)) return new(TradeStatus.InvalidItems, Detail: "Describe the dispute before submitting it.");
            t.DisputeReason = disputeReason.Trim(); t.Status = "Disputed";
        }
        else return new(TradeStatus.NotPending, Detail: "This action is not valid in the trade's current state.");
        t.UpdatedAtUtc = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return new(TradeStatus.Conflict); }
        return new(TradeStatus.Success, Map(t));
    }
    public async Task<TradeResult> StaffCancelAsync(int id, string staff, string note, CancellationToken ct = default)
        => await StaffResolveAsync(id, staff, "Cancelled", note, ct);
    public async Task<TradeResult> StaffResolveAsync(int id, string staff, string resolution, string note, CancellationToken ct = default)
    {
        var t = await Query(true).SingleOrDefaultAsync(x => x.TradeId == id, ct);
        if (t is null) return new(TradeStatus.NotFound);
        if (t.Status is not ("Pending" or "Trading" or "Disputed")) return new(TradeStatus.NotPending, Detail: "Only active trades can be resolved.");
        if (resolution == "Completed")
        {
            if (t.Status == "Pending") return new(TradeStatus.NotPending, Detail: "A pending request cannot be completed by Staff.");
            await TransferQuantitiesAsync(t, ct); UnlockItems(t); t.Status = "Completed";
        }
        else { if (t.Status == "Pending" && t.RequestedItemsLocked) UnlockItems(t, "Requested"); else if (t.Status is "Trading" or "Disputed") UnlockItems(t); t.Status = "Cancelled"; }
        t.UpdatedAtUtc = DateTime.UtcNow; t.StaffResolutionNote = note.Trim(); t.ResolvedByStaffUserName = staff;
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
    private static void LockItems(Trade t, string? side = null) { foreach (var i in t.TradeItems.Where(item => side is null || item.Side == side)) { i.CollectionItem.LockedQuantity += i.Quantity; i.CollectionItem.IsInTrade = true; } }
    private static void UnlockItems(Trade t, string? side = null) { foreach (var i in t.TradeItems.Where(item => side is null || item.Side == side)) { i.CollectionItem.LockedQuantity -= i.Quantity; i.CollectionItem.IsInTrade = i.CollectionItem.LockedQuantity > 0; } }
    private async Task TransferQuantitiesAsync(Trade t, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var tradeItem in t.TradeItems)
        {
            var source = tradeItem.CollectionItem;
            source.CurrentQuantity -= tradeItem.Quantity;
            source.QuantityHistory.Add(new CollectionItemQuantityHistory { Quantity = source.CurrentQuantity, ChangedAtUtc = now });
            var destinationUser = tradeItem.Side == "Offered" ? t.RecipientUserName : t.RequesterUserName;
            var destination = db.CollectionItems.Local.FirstOrDefault(item =>
                !item.IsDeleted && item.UserName == destinationUser && item.ItemTypeId == source.ItemTypeId
                && item.Condition == source.Condition && item.Rarity == source.Rarity);
            destination ??= await db.CollectionItems
                .Include(item => item.CollectionItemCategories)
                .FirstOrDefaultAsync(item => item.UserName == destinationUser && item.ItemTypeId == source.ItemTypeId
                    && item.Condition == source.Condition && item.Rarity == source.Rarity, ct);
            if (destination is null)
            {
                destination = new CollectionItem
                {
                    ItemTypeId = source.ItemTypeId,
                    ItemName = source.ItemName,
                    Condition = source.Condition,
                    Rarity = source.Rarity,
                    ImagePath = source.ImagePath,
                    StartingQuantity = tradeItem.Quantity,
                    CurrentQuantity = tradeItem.Quantity,
                    UserName = destinationUser,
                    CollectionItemCategories = source.CollectionItemCategories.Select(category => new CollectionItemCategory { CategoryCode = category.CategoryCode }).ToList()
                };
                db.CollectionItems.Add(destination);
            }
            else
            {
                destination.CurrentQuantity += tradeItem.Quantity;
            }
            destination.QuantityHistory.Add(new CollectionItemQuantityHistory { Quantity = destination.CurrentQuantity, ChangedAtUtc = now });
            tradeItem.TransferredCollectionItem = destination;
        }
    }
    private static TradeResponse Map(Trade t) => new() { TradeId=t.TradeId,RequesterUserName=t.RequesterUserName,RecipientUserName=t.RecipientUserName,WishlistItemId=t.WishlistItemId,Message=t.Message,Status=t.Status,CreatedAtUtc=t.CreatedAtUtc,UpdatedAtUtc=t.UpdatedAtUtc,StaffResolutionNote=t.StaffResolutionNote,ResolvedByStaffUserName=t.ResolvedByStaffUserName,RequesterConfirmedComplete=t.RequesterConfirmedComplete,RecipientConfirmedComplete=t.RecipientConfirmedComplete,DisputeReason=t.DisputeReason,OfferedItems=t.TradeItems.Where(i=>i.Side=="Offered").Select(i=>MapItem(i,t.RequesterUserName)).ToList(),RequestedItems=t.TradeItems.Where(i=>i.Side=="Requested").Select(i=>MapItem(i,t.RecipientUserName)).ToList() };
    private static TradeItemResponse MapItem(TradeItem i,string owner)=>new(){CollectionItemId=i.CollectionItemId,ItemTypeId=i.CollectionItem.ItemTypeId,ReceivedCollectionItemId=i.TransferredCollectionItemId,ItemName=i.CollectionItem.ItemName,Condition=i.CollectionItem.Condition,Rarity=i.CollectionItem.Rarity,ImagePath=i.CollectionItem.ImagePath,OriginalOwnerUserName=owner,CurrentQuantity=i.CollectionItem.CurrentQuantity,TradeQuantity=i.Quantity,LockedQuantity=i.CollectionItem.LockedQuantity,IsInTrade=i.CollectionItem.IsInTrade};
    private static string? Clean(string? s)=>string.IsNullOrWhiteSpace(s)?null:s.Trim();
}
