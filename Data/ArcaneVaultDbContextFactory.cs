// Name:
// Student Admin No.:
// Tutorial Group:

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArcaneVault.Data;

public class ArcaneVaultDbContextFactory : IDesignTimeDbContextFactory<ArcaneVaultDbContext>
{
    public ArcaneVaultDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ArcaneVaultDbContext>()
            .UseSqlite("Data Source=Data/ArcaneVault.db;Foreign Keys=True")
            .Options;
        return new ArcaneVaultDbContext(options);
    }
}
