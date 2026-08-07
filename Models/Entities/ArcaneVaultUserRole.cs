// Name:
// Student Admin No.:
// Tutorial Group:

namespace ArcaneVault.Models.Entities;

public class ArcaneVaultUserRole
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public ICollection<ArcaneVaultUser> Users { get; set; } = [];
}
