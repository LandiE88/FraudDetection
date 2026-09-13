using FraudDetection.Application.Auth;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Transactions;
using FraudDetection.Infrastructure.Identity;
using FraudDetection.Infrastructure.Persistence;
using FraudDetection.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FraudDetection.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "FraudDetectionDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' was not found in configuration.");

        services.AddDbContext<FraudDetectionDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(FraudDetectionDbContext).Assembly.FullName)));

        services.AddScoped<ITransactionEventRepository, TransactionEventRepository>();
        services.AddScoped<IAccountHolderRepository, AccountHolderRepository>();
        services.AddScoped<IFraudRuleSettingsRepository, FraudRuleSettingsRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();

        AddAuth(services, configuration);

        return services;
    }

    private static void AddAuth(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // AddIdentityCore rather than AddIdentity: this is a stateless JWT API with no
        // server-rendered login page, so the cookie authentication scheme and other
        // UI-oriented pieces AddIdentity pulls in would be unused weight.
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Mirrored by RegisterCommandValidator in the application layer, so a
                // weak password is rejected with a 400 before it ever reaches here.
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<FraudDetectionDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    }
}
