namespace FinancialAPI.Infrastructure.Auth;

/// <summary>
/// Lightweight in-memory user record.
/// In production this would be a proper DB table with ASP.NET Identity.
/// </summary>
public class AppUser
{
    public string Id           { get; set; } = Guid.NewGuid().ToString();
    public string Username     { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;   // BCrypt hash
    public string Role         { get; set; } = "User";         // User | ComplianceOfficer | Admin
    public string? FullName    { get; set; }
    public string? RefreshToken          { get; set; }
    public DateTime? RefreshTokenExpiry  { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}
