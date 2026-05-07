using FinancialAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialAPI.Infrastructure.Persistence.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.AccountId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ReferenceType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ExternalReference).HasMaxLength(100);

        builder.Property(e => e.Amount).HasPrecision(18, 4);
        builder.Property(e => e.BalanceBefore).HasPrecision(18, 4);
        builder.Property(e => e.BalanceAfter).HasPrecision(18, 4);

        builder.Property(e => e.Currency)
               .HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.EntryType)
               .HasConversion<string>().HasMaxLength(10);

        builder.HasIndex(e => e.AccountId);
        builder.HasIndex(e => e.ReferenceId);
        builder.HasIndex(e => e.CreatedAt);
        // Composite index for account balance history queries
        builder.HasIndex(e => new { e.AccountId, e.CreatedAt });

        builder.Ignore(e => e.DomainEvents);
    }
}
