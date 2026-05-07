using FinancialAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialAPI.Infrastructure.Persistence.Configurations;

public class SettlementEntryConfiguration : IEntityTypeConfiguration<SettlementEntry>
{
    public void Configure(EntityTypeBuilder<SettlementEntry> builder)
    {
        builder.ToTable("SettlementEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.GrossAmount).HasPrecision(18, 4);
        builder.Property(e => e.Fee).HasPrecision(18, 4);
        builder.Property(e => e.FailureReason).HasMaxLength(500);

        // NetAmount is a computed property — do not map to a column
        builder.Ignore(e => e.NetAmount);

        builder.HasIndex(e => e.SettlementId);
        builder.HasIndex(e => e.PaymentId);

        builder.Ignore(e => e.DomainEvents);
    }
}
