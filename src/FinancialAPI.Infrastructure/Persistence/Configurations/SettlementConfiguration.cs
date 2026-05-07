using FinancialAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialAPI.Infrastructure.Persistence.Configurations;

public class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("Settlements");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.BatchReference).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ReconciliationReference).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.FailureReason).HasMaxLength(1000);

        builder.Property(s => s.TotalGrossAmount).HasPrecision(18, 4);
        builder.Property(s => s.TotalFees).HasPrecision(18, 4);
        builder.Property(s => s.NetSettlementAmount).HasPrecision(18, 4);

        builder.Property(s => s.Currency)
               .HasConversion<string>().HasMaxLength(10);
        builder.Property(s => s.Status)
               .HasConversion<string>().HasMaxLength(30);

        // One Settlement has many SettlementEntries
        builder.HasMany(s => s.Entries)
               .WithOne()
               .HasForeignKey(e => e.SettlementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.BatchReference).IsUnique();
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.SettlementDate);

        builder.Ignore(s => s.DomainEvents);
    }
}
