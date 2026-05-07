using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialAPI.Infrastructure.Persistence;

/// <summary>
/// Main EF Core DbContext for the Financial Services platform.
/// Handles all 6 domain modules + automatic audit field population.
/// </summary>
public class FinancialDbContext : DbContext
{
    public FinancialDbContext(DbContextOptions<FinancialDbContext> options)
        : base(options) { }

    // ── DbSets (one per entity = one database table) ──────────
    public DbSet<Payment>        Payments        => Set<Payment>();
    public DbSet<Settlement>     Settlements     => Set<Settlement>();
    public DbSet<SettlementEntry> SettlementEntries => Set<SettlementEntry>();
    public DbSet<Beneficiary>    Beneficiaries   => Set<Beneficiary>();
    public DbSet<LedgerEntry>    LedgerEntries   => Set<LedgerEntry>();
    public DbSet<FraudAlert>     FraudAlerts     => Set<FraudAlert>();
    public DbSet<Notification>   Notifications   => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinancialDbContext).Assembly);

        // Global query filter — soft-deleted records are invisible by default
        modelBuilder.Entity<Payment>()     .HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Settlement>()  .HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Beneficiary>() .HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LedgerEntry>() .HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FraudAlert>()  .HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
    }

    // ── Intercept SaveChanges to auto-populate audit fields ───
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                // UpdatedAt is set inside domain methods via SetUpdatedAt()
                // but we ensure it's never null on any update
                if (entry.Entity.UpdatedAt is null)
                    entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
            }
        }
    }
}
