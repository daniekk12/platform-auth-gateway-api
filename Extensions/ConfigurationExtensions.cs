using Platform.Auth.Gateway.Api.Configuration;

namespace Platform.Auth.Gateway.Api.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddGatewayConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));

        services.AddOptions<FunctionEndpointsOptions>()
            .Bind(configuration.GetSection(FunctionEndpointsOptions.SectionName))
            .PostConfigure(options =>
            {
                ApplyEnvironmentOverride(configuration, "SIGNUP_FUNCTION_URL", value => options.SignupUrl = value);
                ApplyEnvironmentOverride(configuration, "LOGIN_FUNCTION_URL", value => options.LoginUrl = value);
            })
            .Validate(
                options => options.TimeoutSeconds > 0,
                $"{FunctionEndpointsOptions.SectionName}.{nameof(FunctionEndpointsOptions.TimeoutSeconds)} must be greater than zero.")
            .ValidateOnStart();

        return services;
    }

    private static void ApplyEnvironmentOverride(
        IConfiguration configuration,
        string environmentVariableName,
        Action<string> apply)
    {
        var value = configuration[environmentVariableName];
        if (!string.IsNullOrWhiteSpace(value))
        {
            apply(value);
        }
    }
}
