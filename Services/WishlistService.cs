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

public class WishlistService(ArcaneVaultDbContext dbContext) : IWishlistService
{
    public async Task<IReadOnlyList<WishlistItemResponse>> GetAllAsync(string? userName, string? excludeUserName, CancellationToken cancellationToken = default)
    {
        var query = dbContext.WishlistItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(userName)) query = query.Where(item => item.UserName == userName.Trim());
        if (!string.IsNullOrWhiteSpace(excludeUserName)) query = query.Where(item => item.UserName != excludeUserName);
        return await Project(query).OrderBy(item => item.UserName).ThenByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<WishlistResult> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Project(dbContext.WishlistItems.AsNoTracking().Where(item => item.WishlistItemId == id)).SingleOrDefaultAsync(cancellationToken);
        return response is null ? new(WishlistStatus.NotFound) : new(WishlistStatus.Success, response);
    }

    public async Task<WishlistResult> CreateAsync(string userName, CreateWishlistItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = new WishlistItem { UserName = userName, ItemName = request.ItemName.Trim(), Notes = NormalizeNotes(request.Notes), DesiredQuantity = request.DesiredQuantity };
        dbContext.WishlistItems.Add(item);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return new(WishlistStatus.Conflict); }
        return await GetByIdAsync(item.WishlistItemId, cancellationToken);
    }

    public async Task<WishlistResult> UpdateAsync(int id, string userName, UpdateWishlistItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.WishlistItems.SingleOrDefaultAsync(item => item.WishlistItemId == id, cancellationToken);
        if (item is null) return new(WishlistStatus.NotFound);
        if (!string.Equals(item.UserName, userName, StringComparison.OrdinalIgnoreCase)) return new(WishlistStatus.Forbidden);
        item.ItemName = request.ItemName.Trim(); item.Notes = NormalizeNotes(request.Notes); item.DesiredQuantity = request.DesiredQuantity;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return new(WishlistStatus.Conflict); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<WishlistResult> DeleteAsync(int id, string userName, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.WishlistItems.SingleOrDefaultAsync(item => item.WishlistItemId == id, cancellationToken);
        if (item is null) return new(WishlistStatus.NotFound);
        if (!string.Equals(item.UserName, userName, StringComparison.OrdinalIgnoreCase)) return new(WishlistStatus.Forbidden);
        if (await dbContext.Trades.AnyAsync(request => request.WishlistItemId == id && request.Status == "Pending", cancellationToken)) return new(WishlistStatus.HasPendingTrades);
        dbContext.WishlistItems.Remove(item);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return new(WishlistStatus.Conflict); }
        return new(WishlistStatus.Success);
    }

    private static IQueryable<WishlistItemResponse> Project(IQueryable<WishlistItem> query) => query.Select(item => new WishlistItemResponse
    {
        WishlistItemId = item.WishlistItemId, ItemName = item.ItemName, Notes = item.Notes, DesiredQuantity = item.DesiredQuantity,
        UserName = item.UserName, CreatedAtUtc = item.CreatedAtUtc, PendingTradeCount = item.Trades.Count(request => request.Status == "Pending")
    });
    private static string? NormalizeNotes(string? notes) => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
