using FinancialAPI.Application.DTOs.Auth;
using FinancialAPI.Infrastructure.Auth;
using FinancialAPI.Shared;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FinancialAPI.UnitTests.Auth;

public class AuthServiceTests
{
    private static AuthService CreateService()
    {
        var store = new InMemoryUserStore();

        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "UnitTestSecretKey_MustBeLongEnough_123456789",
            Issuer = "FinancialAPI",
            Audience = "FinancialAPIClients",
            ExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7
        });

        return new AuthService(
            store,
            jwtSettings,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var service = CreateService();

        var request = new LoginRequest
        {
            Username = "admin",
            Password = "Admin@123"
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Username.Should().Be("admin");
        result.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var service = CreateService();

        var request = new LoginRequest
        {
            Username = "admin",
            Password = "WrongPassword"
        };

        // Act
        var act = async () => await service.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid username or password.");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidUsername_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var service = CreateService();

        var request = new LoginRequest
        {
            Username = "missing-user",
            Password = "Admin@123"
        };

        // Act
        var act = async () => await service.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid username or password.");
    }
}