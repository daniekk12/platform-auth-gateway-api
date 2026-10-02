using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Platform.Auth.Gateway.Api.Configuration;

public sealed class FunctionEndpointsOptionsValidator : IValidateOptions<FunctionEndpointsOptions>
{
    private readonly IHostEnvironment _environment;

    public FunctionEndpointsOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, FunctionEndpointsOptions options)
    {
        var signupResult = ValidateEndpointUrl(
            options.SignupUrl,
            nameof(FunctionEndpointsOptions.SignupUrl));

        if (signupResult is not null)
        {
            return signupResult;
        }

        var loginResult = ValidateEndpointUrl(
            options.LoginUrl,
            nameof(FunctionEndpointsOptions.LoginUrl));

        return loginResult ?? ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult? ValidateEndpointUrl(string url, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return ValidateOptionsResult.Fail(
                $"{FunctionEndpointsOptions.SectionName}.{propertyName} is required.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return ValidateOptionsResult.Fail(
                $"{FunctionEndpointsOptions.SectionName}.{propertyName} must be an absolute URI.");
        }

        if (_environment.IsProduction() && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"{FunctionEndpointsOptions.SectionName}.{propertyName} must use HTTPS in Production.");
        }

        return null;
    }
}
