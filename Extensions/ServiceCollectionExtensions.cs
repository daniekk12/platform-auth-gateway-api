using Microsoft.Extensions.Options;
using Platform.Auth.Gateway.Api.Configuration;
using Platform.Auth.Gateway.Api.Http;
using Platform.Auth.Gateway.Api.Services;

namespace Platform.Auth.Gateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdDelegatingHandler>();

        services.AddHttpClient(AuthFunctionClient.HttpClientName)
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var endpointOptions = serviceProvider.GetRequiredService<IOptions<FunctionEndpointsOptions>>().Value;
                var invocationOptions = serviceProvider.GetRequiredService<IOptions<FunctionInvocationOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(endpointOptions.TimeoutSeconds);
                client.DefaultRequestHeaders.TryAddWithoutValidation(
                    FunctionInvocationOptions.InternalHeaderName,
                    invocationOptions.ApiKey);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

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
