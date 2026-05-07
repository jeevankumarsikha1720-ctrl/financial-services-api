using FinancialAPI.Application.DTOs.Payment;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Application.Services;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Shared;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FinancialAPI.UnitTests.Payments;

public class PaymentServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IRepository<Payment>> _paymentsRepoMock = new();
    private readonly Mock<IRepository<Beneficiary>> _beneficiariesRepoMock = new();

    private readonly Mock<IKafkaProducer<PaymentInitiatedMessage>> _initiatedProducerMock = new();
    private readonly Mock<IKafkaProducer<PaymentStatusChangedMessage>> _statusProducerMock = new();

    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _uowMock.Setup(x => x.Payments)
            .Returns(_paymentsRepoMock.Object);

        _uowMock.Setup(x => x.Beneficiaries)
            .Returns(_beneficiariesRepoMock.Object);

        var kafkaSettings = Options.Create(new KafkaSettings
        {
            Topics = new KafkaTopics
            {
                PaymentEvents = "payment-events"
            }
        });

        _service = new PaymentService(
            _uowMock.Object,
            _initiatedProducerMock.Object,
            _statusProducerMock.Object,
            kafkaSettings,
            NullLogger<PaymentService>.Instance);
    }

    [Fact]
    public async Task InitiateAsync_WithValidRequest_CreatesPayment()
    {
        // Arrange
        var beneficiary = Beneficiary.Create(
         ownerId: "user-001",
         firstName: "Test",
         lastName: "Beneficiary",
         email: "beneficiary@test.com",
         phoneNumber: "1234567890",
         accountNumber: "987654321",
         iban: null,
         swiftBic: "HDFCUS33",
         bankName: "HDFC Bank",
         bankCountryCode: "US",
         preferredCurrency: CurrencyCode.USD,
         nickname: "Test Ben",
         createdBy: "user-001");

        beneficiary.SubmitForReview();
        beneficiary.Verify("compliance-001");
        beneficiary.Activate();

        _beneficiariesRepoMock
            .Setup(x => x.GetByIdAsync(
                beneficiary.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(beneficiary);

        var request = new InitiatePaymentRequest
        {
            SenderAccountId = "ACC-001",
            SenderName = "Jeevan",
            BeneficiaryId = beneficiary.Id,
            BeneficiaryAccountNumber = "987654321",
            BeneficiaryBankCode = "HDFC001",
            Amount = 500,
            Currency = CurrencyCode.USD,
            Type = PaymentType.Domestic,
            Description = "Unit test payment"
        };

        // Act
        var result = await _service.InitiateAsync(
            request,
            "user-001");

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(500);
        result.SenderAccountId.Should().Be("ACC-001");

        _paymentsRepoMock.Verify(
            x => x.AddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _uowMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _initiatedProducerMock.Verify(
            x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PaymentInitiatedMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitiateAsync_WithMissingBeneficiary_ThrowsException()
    {
        // Arrange
        _beneficiariesRepoMock
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beneficiary?)null);

        var request = new InitiatePaymentRequest
        {
            SenderAccountId = "ACC-001",
            SenderName = "Jeevan",
            BeneficiaryId = Guid.NewGuid(),
            BeneficiaryAccountNumber = "987654321",
            BeneficiaryBankCode = "HDFC001",
            Amount = 500,
            Currency = CurrencyCode.USD,
            Type = PaymentType.Domestic
        };

        // Act
        var act = async () =>
            await _service.InitiateAsync(request, "user-001");

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsException()
    {
        // Arrange
        _paymentsRepoMock
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        // Act
        var act = async () =>
            await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>();
    }
}