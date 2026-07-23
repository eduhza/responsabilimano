namespace ResponsabiliMano.Infrastructure.Identity;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 12);
    }

    public static bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
    }
}
