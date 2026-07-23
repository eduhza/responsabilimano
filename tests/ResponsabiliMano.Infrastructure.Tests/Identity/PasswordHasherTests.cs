using ResponsabiliMano.Infrastructure.Identity;

namespace ResponsabiliMano.Infrastructure.Tests.Identity;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ReturnsNonEmptyValueDifferentFromPassword()
    {
        var hash = PasswordHasher.Hash("Sup3rSecret!");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("Sup3rSecret!", hash);
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForSamePassword()
    {
        var first = PasswordHasher.Hash("same-password");
        var second = PasswordHasher.Hash("same-password");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = PasswordHasher.Hash("correct horse battery staple");

        Assert.True(PasswordHasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = PasswordHasher.Hash("correct horse battery staple");

        Assert.False(PasswordHasher.Verify("wrong password", hash));
    }

    [Fact]
    public void Verify_IsCaseSensitive()
    {
        var hash = PasswordHasher.Hash("CaseSensitive");

        Assert.False(PasswordHasher.Verify("casesensitive", hash));
    }
}
