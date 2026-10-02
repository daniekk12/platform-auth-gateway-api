using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Platform.Auth.Gateway.Api.Tests;

public sealed class ConfigurationValidationTests
{
    [Fact]
    public void Host_fails_to_start_when_internal_api_key_is_missing()
    {
        var exception = Assert.ThrowsAny<Exception>(() =>
            new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    ConfigureValidFunctionEndpoints(builder);
                    builder.UseSetting("FunctionInvocation:ApiKey", string.Empty);
                })
                .CreateClient());

        Assert.Contains("ApiKey", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Host_fails_to_start_when_signup_url_is_missing()
    {
        var exception = Assert.ThrowsAny<Exception>(() =>
            new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("FunctionInvocation:ApiKey", "test-internal-key");
                    builder.UseSetting("FunctionEndpoints:LoginUrl", "https://login.test");
                    builder.UseSetting("FunctionEndpoints:TimeoutSeconds", "30");
                    builder.UseSetting("FunctionEndpoints:SignupUrl", string.Empty);
                })
                .CreateClient());

        Assert.Contains("SignupUrl", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureValidFunctionEndpoints(IWebHostBuilder builder)
    {
        builder.UseSetting("FunctionInvocation:ApiKey", "test-internal-key");
        builder.UseSetting("FunctionEndpoints:SignupUrl", "https://signup.test");
        builder.UseSetting("FunctionEndpoints:LoginUrl", "https://login.test");
        builder.UseSetting("FunctionEndpoints:TimeoutSeconds", "30");
    }
}
