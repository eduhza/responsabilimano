using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Services;

namespace ResponsabiliMano.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResponsabiliManoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEmailService, LoggingEmailService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        return services;
    }
}
