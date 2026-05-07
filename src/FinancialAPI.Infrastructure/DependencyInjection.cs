using FinancialAPI.Infrastructure.Auth;
using FinancialAPI.Application.DTOs.Beneficiary;
using FinancialAPI.Application.DTOs.Fraud;
using FinancialAPI.Application.DTOs.Payment;
using FinancialAPI.Application.DTOs.Settlement;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Application.Services;
using FinancialAPI.Infrastructure.Kafka;
using FinancialAPI.Infrastructure.Kafka.Consumers;
using FinancialAPI.Infrastructure.Persistence;
using FinancialAPI.Infrastructure.Repositories;
using FinancialAPI.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialAPI.Infrastructure;

/// <summary>
/// Registers all Infrastructure services: Database, Repositories, Kafka.
/// Called from Program.cs: builder.Services.AddInfrastructure(config)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────
        services.AddDbContext<FinancialDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly("FinancialAPI.Infrastructure");
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                    npgsql.CommandTimeout(30);
                });

            options.EnableSensitiveDataLogging(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development");
        });

        // ── Repositories ──────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.Configure<JwtSettings>(
        configuration.GetSection("JwtSettings"));
        // ── Auth Services ─────────────────────────────────────────
        services.AddSingleton<InMemoryUserStore>();
        services.AddScoped<IAuthService, AuthService>();
        // ── Kafka Settings ────────────────────────────────────
        services.Configure<KafkaSettings>(
            configuration.GetSection(KafkaSettings.SectionName));

        // ── Kafka Producer (generic, one instance per type) ───
        services.AddSingleton(typeof(IKafkaProducer<>), typeof(KafkaProducer<>));

        // ── Payment Services ──────────────────────────────────
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IKafkaConsumerHandler<PaymentInitiatedMessage>, PaymentInitiatedHandler>();
        services.AddScoped<IKafkaConsumerHandler<PaymentStatusChangedMessage>, PaymentStatusChangedHandler>();
        services.AddHostedService<PaymentInitiatedConsumer>();

        // ── Settlement Services ───────────────────────────────
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IKafkaConsumerHandler<SettlementCompletedMessage>, SettlementCompletedHandler>();
        services.AddHostedService<SettlementConsumer>();

        // ── Beneficiary Services ──────────────────────────────
        services.AddScoped<IBeneficiaryService, BeneficiaryService>();
        services.AddScoped<IKafkaConsumerHandler<BeneficiaryCreatedMessage>, BeneficiaryCreatedHandler>();
        services.AddScoped<IKafkaConsumerHandler<BeneficiaryVerifiedMessage>, BeneficiaryVerifiedHandler>();
        services.AddHostedService<BeneficiaryConsumer>();

        // ── Ledger Services ───────────────────────────────────
        services.AddScoped<ILedgerService, LedgerService>();

        // ── Fraud Detection Services ──────────────────────────
        services.AddScoped<IFraudService, FraudService>();
        services.AddScoped<IKafkaConsumerHandler<FraudAlertRaisedMessage>, FraudAlertRaisedHandler>();
        services.AddScoped<IKafkaConsumerHandler<FraudAlertResolvedMessage>, FraudAlertResolvedHandler>();
        services.AddHostedService<FraudAlertRaisedConsumer>();

        // ── Notification Services ─────────────────────────────
        services.AddScoped<INotificationService, NotificationService>();

        // ── Kafka Health Check ────────────────────────────────
        services.AddSingleton<KafkaHealthCheck>();

        return services;
    }
}
