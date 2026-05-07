using FinancialAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialAPI.Infrastructure.Persistence.Configurations;

public class FraudAlertConfiguration : IEntityTypeConfiguration<FraudAlert>
{
    public void Configure(EntityTypeBuilder<FraudAlert> builder)
    {
        builder.ToTable("FraudAlerts");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.AccountId).IsRequired().HasMaxLength(100);
        builder.Property(f => f.AlertDescription).HasMaxLength(1000);
        builder.Property(f => f.ReviewedBy).HasMaxLength(100);
        builder.Property(f => f.ReviewNotes).HasMaxLength(1000);

        builder.Property(f => f.TransactionAmount).HasPrecision(18, 4);

        builder.Property(f => f.Currency)
               .HasConversion<string>().HasMaxLength(10);
        builder.Property(f => f.RiskLevel)
               .HasConversion<string>().HasMaxLength(20);

        // Store string[] RiskFactors as a JSON column in PostgreSQL
        builder.Property(f => f.RiskFactors)
               .HasColumnType("jsonb");

        builder.HasIndex(f => f.PaymentId);
        builder.HasIndex(f => f.AccountId);
        builder.HasIndex(f => f.RiskLevel);
        builder.HasIndex(f => f.IsResolved);

        builder.Ignore(f => f.DomainEvents);
    }
}
