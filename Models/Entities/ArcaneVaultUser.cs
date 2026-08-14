// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class ArcaneVaultUser
{
    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int RoleId { get; set; }

    public ArcaneVaultUserRole Role { get; set; } = null!;

    public ICollection<CollectionItem> CollectionItems { get; set; } = [];

    public ICollection<WishlistItem> WishlistItems { get; set; } = [];
}
