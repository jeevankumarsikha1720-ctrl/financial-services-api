using FinancialAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialAPI.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.UserId).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.Channel).IsRequired().HasMaxLength(20);
        builder.Property(n => n.ReferenceType).HasMaxLength(50);
        builder.Property(n => n.DeliveryError).HasMaxLength(500);

        builder.Property(n => n.Type)
               .HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.Type);
        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.CreatedAt);
        // Composite: get unread notifications for a user fast
        builder.HasIndex(n => new { n.UserId, n.IsRead });

        builder.Ignore(n => n.DomainEvents);
    }
}
