using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Platform.Auth.Gateway.Api.Tests;

public sealed class AuthControllerValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthControllerValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(ConfigureRequiredSettings).CreateClient();
    }

    [Fact]
    public async Task Signup_rejects_invalid_email_without_calling_downstream()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/signup",
            new { email = "not-an-email", password = "Password123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Password123!", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_rejects_missing_password()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new { email = "user@example.com", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Auth_responses_include_no_store_cache_control()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new { email = "user@example.com", password = "short" });

        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    internal static void ConfigureRequiredSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("FunctionInvocation:ApiKey", "test-internal-key");
        builder.UseSetting("FunctionEndpoints:SignupUrl", "https://signup.test");
        builder.UseSetting("FunctionEndpoints:LoginUrl", "https://login.test");
        builder.UseSetting("FunctionEndpoints:TimeoutSeconds", "30");
    }
}
