using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Configuration;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Services;

namespace ResponsabiliMano.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResponsabiliManoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (connectionString.Contains("DataSource=", StringComparison.OrdinalIgnoreCase)
                || connectionString.Contains("Version=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        // Email: send over SMTP when a password is configured (production, via
        // Secret Manager); otherwise fall back to logging (local dev sends nothing).
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>() ?? new EmailSettings();
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        if (emailSettings.IsConfigured)
            services.AddScoped<IEmailService, SmtpEmailService>();
        else
            services.AddScoped<IEmailService, LoggingEmailService>();

        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<ICheckInService, CheckInService>();
        services.AddScoped<ICheckInNotificationService, CheckInNotificationService>();

        return services;
    }
}
