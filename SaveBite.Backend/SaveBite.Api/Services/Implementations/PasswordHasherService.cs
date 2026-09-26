using System.Text;
using BCrypt.Net;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public class PasswordHasherService : IPasswordHasherService
{
    private const int WorkFactor = 12;
    private const int BcryptMaximumPasswordBytes = 72;

    public string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (password.Length == 0)
            throw new ArgumentException(
                "Password cannot be empty.",
                nameof(password));

        if (Encoding.UTF8.GetByteCount(password) >
            BcryptMaximumPasswordBytes)
        {
            throw new ArgumentException(
                "Password exceeds BCrypt's 72-byte limit.",
                nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(
            password,
            workFactor: WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) ||
            string.IsNullOrWhiteSpace(hashedPassword) ||
            Encoding.UTF8.GetByteCount(password) >
            BcryptMaximumPasswordBytes)
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (SaltParseException)
        {
            return false;
        }
    }
}
