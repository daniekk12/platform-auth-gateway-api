using Microsoft.Extensions.Options;
using Platform.Auth.Gateway.Api.Configuration;
using Platform.Auth.Gateway.Api.Services;

namespace Platform.Auth.Gateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayServices(this IServiceCollection services)
    {
        services.AddHttpClient(AuthFunctionClient.HttpClientName)
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<FunctionEndpointsOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        services.AddSingleton<IAuthFunctionClient, AuthFunctionClient>();

        return services;
    }

    public static IServiceCollection AddGatewayCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSection = configuration.GetSection(CorsOptions.SectionName);
        var allowedOrigins = corsSection.Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0)
        {
            return services;
        }

        services.AddCors(options =>
        {
            options.AddPolicy(
                CorsOptions.SectionName,
                policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        return services;
    }
}
