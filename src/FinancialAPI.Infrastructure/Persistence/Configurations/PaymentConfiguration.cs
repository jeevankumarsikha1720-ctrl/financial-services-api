using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialAPI.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        // Primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever(); // We set it in domain

        // Required string columns with max lengths
        builder.Property(p => p.ReferenceNumber)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.SenderAccountId)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(p => p.SenderName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(p => p.BeneficiaryAccountNumber)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.BeneficiaryBankCode)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(p => p.Description)
               .HasMaxLength(500);

        builder.Property(p => p.FailureReason)
               .HasMaxLength(1000);

        // Decimal precision — important for financial data!
        // precision: 18 digits total, 4 after decimal point
        builder.Property(p => p.Amount)
               .HasPrecision(18, 4);

        // Store enums as strings for readability in DB
        builder.Property(p => p.Currency)
               .HasConversion<string>()
               .HasMaxLength(10);

        builder.Property(p => p.Type)
               .HasConversion<string>()
               .HasMaxLength(30);

        builder.Property(p => p.Status)
               .HasConversion<string>()
               .HasMaxLength(30);

        // Indexes for common queries
        builder.HasIndex(p => p.ReferenceNumber).IsUnique();
        builder.HasIndex(p => p.SenderAccountId);
        builder.HasIndex(p => p.BeneficiaryId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CreatedAt);

        // Ignore domain events — they are not persisted
        builder.Ignore(p => p.DomainEvents);
    }
}
