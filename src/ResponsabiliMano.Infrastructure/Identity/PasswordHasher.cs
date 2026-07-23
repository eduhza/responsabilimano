namespace ResponsabiliMano.Infrastructure.Identity;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 12);
    }

    public static bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A stored hash that cannot be parsed must fail verification rather than
            // surface as an unhandled 500 during login.
            return false;
        }
    }
}
