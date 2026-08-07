// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.Data;

public class ArcaneVaultDbContext(DbContextOptions<ArcaneVaultDbContext> options)
    : DbContext(options)
{
    public DbSet<ArcaneVaultUser> ArcaneVaultUsers => Set<ArcaneVaultUser>();

    public DbSet<ArcaneVaultUserRole> ArcaneVaultUserRoles => Set<ArcaneVaultUserRole>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CollectionItem> CollectionItems => Set<CollectionItem>();

    public DbSet<CollectionItemCategory> CollectionItemCategories => Set<CollectionItemCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserRoles(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureCategories(modelBuilder);
        ConfigureCollectionItems(modelBuilder);
        ConfigureCollectionItemCategories(modelBuilder);
    }

    private static void ConfigureUserRoles(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ArcaneVaultUserRole>();

        entity.ToTable("ArcaneVaultUserRoles");
        entity.HasKey(role => role.RoleId);
        entity.Property(role => role.RoleName)
            .HasMaxLength(20)
            .IsRequired();
        entity.HasIndex(role => role.RoleName)
            .IsUnique();
        entity.HasData(
            new ArcaneVaultUserRole { RoleId = 1, RoleName = "User" },
            new ArcaneVaultUserRole { RoleId = 2, RoleName = "Staff" });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ArcaneVaultUser>();

        entity.ToTable("ArcaneVaultUsers");
        entity.HasKey(user => user.UserName);
        entity.Property(user => user.UserName)
            .HasMaxLength(50)
            .UseCollation("NOCASE");
        entity.Property(user => user.Email)
            .HasMaxLength(254)
            .UseCollation("NOCASE")
            .IsRequired();
        entity.Property(user => user.PasswordHash)
            .IsRequired();
        entity.Property(user => user.IsDeleted)
            .HasDefaultValue(false);
        entity.HasIndex(user => user.Email)
            .IsUnique();
        entity.HasOne(user => user.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasQueryFilter(user => !user.IsDeleted);
    }

    private static void ConfigureCategories(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Category>();

        entity.ToTable("Categories");
        entity.HasKey(category => category.CategoryCode);
        entity.Property(category => category.CategoryCode)
            .HasMaxLength(20)
            .UseCollation("NOCASE");
        entity.Property(category => category.CategoryName)
            .HasMaxLength(100)
            .UseCollation("NOCASE")
            .IsRequired();
        entity.HasIndex(category => category.CategoryName)
            .IsUnique();
    }

    private static void ConfigureCollectionItems(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CollectionItem>();

        entity.ToTable("CollectionItems");
        entity.HasKey(item => item.ItemId);
        entity.Property(item => item.ItemName)
            .HasMaxLength(150)
            .IsRequired();
        entity.Property(item => item.IsDeleted)
            .HasDefaultValue(false);
        entity.Property(item => item.UserName)
            .HasMaxLength(50)
            .UseCollation("NOCASE")
            .IsRequired();
        entity.HasOne(item => item.User)
            .WithMany(user => user.CollectionItems)
            .HasForeignKey(item => item.UserName)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasQueryFilter(item => !item.IsDeleted);

        entity.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_CollectionItems_StartingQuantity_NonNegative",
                "StartingQuantity >= 0");
            tableBuilder.HasCheckConstraint(
                "CK_CollectionItems_CurrentQuantity_NonNegative",
                "CurrentQuantity >= 0");
        });
    }

    private static void ConfigureCollectionItemCategories(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CollectionItemCategory>();

        entity.ToTable("CollectionItemCategories");
        entity.HasKey(link => new { link.ItemId, link.CategoryCode });
        entity.Property(link => link.CategoryCode)
            .HasMaxLength(20)
            .UseCollation("NOCASE");
        entity.HasOne(link => link.CollectionItem)
            .WithMany(item => item.CollectionItemCategories)
            .HasForeignKey(link => link.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(link => link.Category)
            .WithMany(category => category.CollectionItemCategories)
            .HasForeignKey(link => link.CategoryCode)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasQueryFilter(link => !link.CollectionItem.IsDeleted);
    }
}
