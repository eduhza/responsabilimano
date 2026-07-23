using ResponsabiliMano.Core.Entities;

namespace ResponsabiliMano.Core.Services;

public interface IUserLoginService
{
    Task<User?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
