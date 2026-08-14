// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class WishlistItem
{
    public int WishlistItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int DesiredQuantity { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string UserName { get; set; } = string.Empty;
    public ArcaneVaultUser User { get; set; } = null!;
    public ICollection<Trade> Trades { get; set; } = [];
}
