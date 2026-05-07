using FinancialAPI.Application.DTOs.Auth;

namespace FinancialAPI.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<UserResponse> GetCurrentUserAsync(string userId, CancellationToken ct = default);
}
