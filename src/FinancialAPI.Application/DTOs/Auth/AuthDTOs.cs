using System.ComponentModel.DataAnnotations;

namespace FinancialAPI.Application.DTOs.Auth;

// ── Requests ──────────────────────────────────────────────────────────────────

public class LoginRequest
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required] [MinLength(3)]  public string Username    { get; set; } = string.Empty;
    [Required] [EmailAddress]  public string Email       { get; set; } = string.Empty;
    [Required] [MinLength(8)]  public string Password    { get; set; } = string.Empty;
    [Required]                 public string Role        { get; set; } = "User"; // User | ComplianceOfficer | Admin
    public string? FullName { get; set; }
}

public class RefreshTokenRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

// ── Responses ─────────────────────────────────────────────────────────────────

public class AuthResponse
{
    public string AccessToken  { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt  { get; set; }
    public string UserId       { get; set; } = string.Empty;
    public string Username     { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string Role         { get; set; } = string.Empty;
}

public class UserResponse
{
    public string UserId   { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Role     { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
}
