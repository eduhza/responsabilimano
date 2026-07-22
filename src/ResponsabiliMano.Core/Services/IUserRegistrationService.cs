using ResponsabiliMano.Core.Entities;

namespace ResponsabiliMano.Core.Services;

public interface IUserRegistrationService
{
    Task<User> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);
}
