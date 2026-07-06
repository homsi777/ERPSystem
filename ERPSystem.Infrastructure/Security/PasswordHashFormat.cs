namespace ERPSystem.Infrastructure.Security;

internal static class PasswordHashFormat
{
    public static bool IsBcryptHash(string? passwordHash) =>
        !string.IsNullOrWhiteSpace(passwordHash)
        && passwordHash.Length >= 60
        && (passwordHash.StartsWith("$2a$", StringComparison.Ordinal)
            || passwordHash.StartsWith("$2b$", StringComparison.Ordinal)
            || passwordHash.StartsWith("$2y$", StringComparison.Ordinal));
}
