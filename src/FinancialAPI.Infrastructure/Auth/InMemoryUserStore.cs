namespace FinancialAPI.Infrastructure.Auth;

/// <summary>
/// Thread-safe in-memory user store.
/// Singleton — lives for the lifetime of the application.
/// In production, replace with a proper User table + EF Core.
/// </summary>
public class InMemoryUserStore
{
    private readonly List<AppUser> _users = [];
    private readonly object _lock = new();

    public InMemoryUserStore()
    {
        // Seed three built-in test users on startup
        SeedUsers();
    }

    public AppUser? FindByUsername(string username)
    {
        lock (_lock)
            return _users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public AppUser? FindById(string id)
    {
        lock (_lock)
            return _users.FirstOrDefault(u => u.Id == id);
    }

    public AppUser? FindByRefreshToken(string refreshToken)
    {
        lock (_lock)
            return _users.FirstOrDefault(u => u.RefreshToken == refreshToken);
    }

    public bool UsernameExists(string username)
    {
        lock (_lock)
            return _users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public bool EmailExists(string email)
    {
        lock (_lock)
            return _users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public void Add(AppUser user)
    {
        lock (_lock)
            _users.Add(user);
    }

    public void Update(AppUser user)
    {
        // Already a reference — nothing to do for in-memory store.
        // This method exists so callers have a consistent API.
    }

    // ── Seed data ─────────────────────────────────────────────────────────────

    private void SeedUsers()
    {
        _users.AddRange([
            new AppUser
            {
                Id           = "user-001",
                Username     = "admin",
                Email        = "admin@financialapi.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role         = "Admin",
                FullName     = "System Administrator"
            },
            new AppUser
            {
                Id           = "user-002",
                Username     = "compliance",
                Email        = "compliance@financialapi.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Compliance@123"),
                Role         = "ComplianceOfficer",
                FullName     = "Compliance Officer"
            },
            new AppUser
            {
                Id           = "user-003",
                Username     = "jeevan",
                Email        = "jeevankumarsikha1720@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Jeevan@123"),
                Role         = "User",
                FullName     = "Jeevan Kumar"
            }
        ]);
    }
}
