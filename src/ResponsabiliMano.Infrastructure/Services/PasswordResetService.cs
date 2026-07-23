using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Identity;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordResetService> _logger;

    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public PasswordResetService(AppDbContext context, IEmailService emailService, ILogger<PasswordResetService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task RequestResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Password reset requested for unknown email: {Email}", normalizedEmail);
            return;
        }

        var rawToken = GenerateToken();
        var tokenHash = HashToken(rawToken);

        var resetToken = new Core.Entities.PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.Add(TokenLifetime),
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync(cancellationToken);

        var resetLink = $"https://localhost:8080/reset-password?token={rawToken}";
        var subject = "Recuperacao de Senha - ResponsabiliMano";
        var body = $"""
            <h2>Recuperacao de Senha</h2>
            <p>Ola, {user.Name}!</p>
            <p>Voce solicitou a redefinicao de sua senha. Clique no link abaixo para definir uma nova senha:</p>
            <p><a href="{resetLink}">{resetLink}</a></p>
            <p>Este link expira em 1 hora.</p>
            <p>Se voce nao solicitou esta redefinicao, ignore este e-mail.</p>
            """;

        await _emailService.SendEmailAsync(user.Email, subject, body, cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var tokenHash = HashToken(token);

        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null || resetToken.UsedAt is not null || resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken);

        if (user is null)
            return false;

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
