namespace FinancialAPI.Shared;

/// <summary>
/// Strongly-typed JWT configuration — bound from appsettings.json "JwtSettings" section.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret        { get; set; } = string.Empty;
    public string Issuer        { get; set; } = "FinancialAPI";
    public string Audience      { get; set; } = "FinancialAPIClients";
    public int    ExpiryMinutes { get; set; } = 60;
    public int    RefreshTokenExpiryDays { get; set; } = 7;
}
