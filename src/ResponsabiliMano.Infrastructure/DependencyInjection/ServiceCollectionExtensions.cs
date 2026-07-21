using Microsoft.Extensions.DependencyInjection;

namespace ResponsabiliMano.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResponsabiliManoInfrastructure(this IServiceCollection services)
    {
        // TODO: register EF Core, repositories, MailKit services in S0.2/S1.x
        return services;
    }
}
