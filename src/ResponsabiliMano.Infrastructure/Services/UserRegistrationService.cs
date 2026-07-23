using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Identity;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class UserRegistrationService : IUserRegistrationService
{
    private readonly AppDbContext _context;

    public UserRegistrationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailAddress.Normalize(email);

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(password),
            PreferredLanguage = "pt-BR",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
}
