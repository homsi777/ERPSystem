using ERPSystem.Application.Abstractions.Services;

namespace ERPSystem.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword);

    public bool VerifyPassword(string plainPassword, string passwordHash)
    {
        if (!PasswordHashFormat.IsBcryptHash(passwordHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}
