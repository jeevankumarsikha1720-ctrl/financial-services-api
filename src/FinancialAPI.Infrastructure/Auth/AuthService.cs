using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FinancialAPI.Application.DTOs.Auth;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FinancialAPI.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly InMemoryUserStore _store;
    private readonly JwtSettings _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        InMemoryUserStore store,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _store  = store;
        _jwt    = jwtSettings.Value;
        _logger = logger;
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = _store.FindByUsername(request.Username)
            ?? throw new UnauthorizedAccessException("Invalid username or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        var response = IssueTokens(user);
        _logger.LogInformation("User logged in. Username: {Username}, Role: {Role}",
            user.Username, user.Role);

        return Task.FromResult(response);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (_store.UsernameExists(request.Username))
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");

        if (_store.EmailExists(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        // Validate role
        var validRoles = new[] { "User", "ComplianceOfficer", "Admin" };
        if (!validRoles.Contains(request.Role))
            throw new ArgumentException($"Invalid role '{request.Role}'. Must be one of: {string.Join(", ", validRoles)}.");

        var newUser = new AppUser
        {
            Username     = request.Username,
            Email        = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = request.Role,
            FullName     = request.FullName
        };

        _store.Add(newUser);

        var response = IssueTokens(newUser);
        _logger.LogInformation("New user registered. Username: {Username}, Role: {Role}",
            newUser.Username, newUser.Role);

        return Task.FromResult(response);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    public Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var user = _store.FindByRefreshToken(request.RefreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");

        var response = IssueTokens(user);
        _logger.LogInformation("Token refreshed for user: {Username}", user.Username);

        return Task.FromResult(response);
    }

    // ── Get Current User ──────────────────────────────────────────────────────

    public Task<UserResponse> GetCurrentUserAsync(string userId, CancellationToken ct = default)
    {
        var user = _store.FindById(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        return Task.FromResult(new UserResponse
        {
            UserId    = user.Id,
            Username  = user.Username,
            Email     = user.Email,
            Role      = user.Role,
            FullName  = user.FullName,
            CreatedAt = user.CreatedAt
        });
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private AuthResponse IssueTokens(AppUser user)
    {
        var accessToken  = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var expiresAt    = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

        // Persist refresh token
        user.RefreshToken       = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays);
        _store.Update(user);

        return new AuthResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt    = expiresAt,
            UserId       = user.Id,
            Username     = user.Username,
            Email        = user.Email,
            Role         = user.Role
        };
    }

    private string GenerateAccessToken(AppUser user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name,               user.Username),
            new Claim(ClaimTypes.NameIdentifier,     user.Id),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim("username",                    user.Username),
            new Claim("fullname",                    user.FullName ?? user.Username),
        };

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
