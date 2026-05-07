using FinancialAPI.Application.DTOs.Fraud;
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

namespace FinancialAPI.UnitTests.Fraud;

public class FraudServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IRepository<FraudAlert>> _fraudRepoMock = new();

    private readonly Mock<IKafkaProducer<FraudAlertRaisedMessage>> _raisedProducerMock = new();
    private readonly Mock<IKafkaProducer<FraudAlertResolvedMessage>> _resolvedProducerMock = new();

    private readonly FraudService _service;

    public FraudServiceTests()
    {
        _uowMock.Setup(x => x.FraudAlerts)
            .Returns(_fraudRepoMock.Object);

        var kafkaSettings = Options.Create(new KafkaSettings
        {
            Topics = new KafkaTopics
            {
                FraudAlerts = "fraud-alerts"
            }
        });

        _service = new FraudService(
            _uowMock.Object,
            _raisedProducerMock.Object,
            _resolvedProducerMock.Object,
            kafkaSettings,
            NullLogger<FraudService>.Instance);
    }

    [Fact]
    public async Task RaiseAlertAsync_WithHighRiskScore_CreatesAlertAndPublishesEvent()
    {
        var request = new RaiseFraudAlertRequest
        {
            PaymentId = Guid.NewGuid(),
            AccountId = "ACC-001",
            RiskScore = 0.85,
            RiskFactors = ["Large transaction", "Unusual location"],
            TransactionAmount = 5000,
            Currency = CurrencyCode.USD
        };

        var result = await _service.RaiseAlertAsync(request);

        result.Should().NotBeNull();
        result.RiskScore.Should().Be(0.85);
        result.PaymentBlocked.Should().BeTrue();

        _fraudRepoMock.Verify(
            x => x.AddAsync(It.IsAny<FraudAlert>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _uowMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _raisedProducerMock.Verify(
            x => x.ProduceAsync(
                "fraud-alerts",
                It.IsAny<string>(),
                It.IsAny<FraudAlertRaisedMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RaiseAlertAsync_WithLowRiskScore_ThrowsArgumentException()
    {
        var request = new RaiseFraudAlertRequest
        {
            PaymentId = Guid.NewGuid(),
            AccountId = "ACC-001",
            RiskScore = 0.25,
            RiskFactors = ["Minor risk"],
            TransactionAmount = 100,
            Currency = CurrencyCode.USD
        };

        var act = async () => await _service.RaiseAlertAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Risk score must be above 0.4 to raise a fraud alert.");

        _fraudRepoMock.Verify(
            x => x.AddAsync(It.IsAny<FraudAlert>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingAlert_ThrowsKeyNotFoundException()
    {
        _fraudRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudAlert?)null);

        var act = async () => await _service.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}