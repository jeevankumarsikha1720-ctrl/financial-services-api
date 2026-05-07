using FinancialAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialAPI.Infrastructure.Persistence.Configurations;

public class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
{
    public void Configure(EntityTypeBuilder<Beneficiary> builder)
    {
        builder.ToTable("Beneficiaries");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.OwnerId).IsRequired().HasMaxLength(100);
        builder.Property(b => b.Nickname).IsRequired().HasMaxLength(100);
        builder.Property(b => b.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(b => b.LastName).IsRequired().HasMaxLength(100);
        builder.Property(b => b.Email).IsRequired().HasMaxLength(200);
        builder.Property(b => b.PhoneNumber).HasMaxLength(20);
        builder.Property(b => b.AccountNumber).IsRequired().HasMaxLength(50);
        builder.Property(b => b.IBAN).HasMaxLength(34);
        builder.Property(b => b.SwiftBic).IsRequired().HasMaxLength(11);
        builder.Property(b => b.BankName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.BankCountryCode).IsRequired().HasMaxLength(2);
        builder.Property(b => b.VerifiedBy).HasMaxLength(100);
        builder.Property(b => b.RejectionReason).HasMaxLength(500);

        builder.Property(b => b.PreferredCurrency)
               .HasConversion<string>().HasMaxLength(10);
        builder.Property(b => b.Status)
               .HasConversion<string>().HasMaxLength(30);

        // FullName is a computed C# property — not stored in DB
        builder.Ignore(b => b.FullName);

        builder.HasIndex(b => b.OwnerId);
        builder.HasIndex(b => b.AccountNumber);
        builder.HasIndex(b => b.IBAN);
        builder.HasIndex(b => b.Status);
        // Composite: owner cannot add same account twice
        builder.HasIndex(b => new { b.OwnerId, b.AccountNumber }).IsUnique();

        builder.Ignore(b => b.DomainEvents);
    }
}
