using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Identity;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class UserLoginService : IUserLoginService
{
    private readonly AppDbContext _context;

    public UserLoginService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailAddress.Normalize(email);
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }
}
