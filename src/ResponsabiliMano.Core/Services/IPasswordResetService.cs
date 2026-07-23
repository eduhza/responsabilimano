namespace ResponsabiliMano.Core.Services;

public interface IPasswordResetService
{
    Task RequestResetAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}
