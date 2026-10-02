using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Platform.Auth.Gateway.Api.Contracts;
using Platform.Auth.Gateway.Api.Services;

namespace Platform.Auth.Gateway.Api.Tests;

public sealed class GatewayProductionReadinessTests
{
    [Fact]
    public async Task Cors_allows_configured_origin_on_preflight()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/auth/login");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains("http://localhost:4200", origins);
    }

    [Fact]
    public async Task Downstream_error_response_does_not_expose_internal_function_url()
    {
        await using var factory = CreateFactory(services =>
        {
            services.RemoveAll<IAuthFunctionClient>();
            services.AddSingleton<IAuthFunctionClient>(new StubAuthFunctionClient(
                login: FunctionCallResult<LoginResponse>.DownstreamError(
                    StatusCodes.Status400BadRequest,
                    """{"title":"bad","detail":"failed at https://internal-signup.function.aws/invoke"}""")));
        });

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new { email = "user@example.com", password = "Password123!" });

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("https://internal-signup.function.aws", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Request rejected", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Auth_controller_does_not_log_password()
    {
        var loggerProvider = new CollectingLoggerProvider();
        await using var factory = CreateFactory(services =>
        {
            services.RemoveAll<IAuthFunctionClient>();
            services.AddSingleton<IAuthFunctionClient>(new StubAuthFunctionClient(
                FunctionCallResult<SignupResponse>.Ok(
                    new SignupResponse("ok", "user@example.com"))));
            services.AddLogging(builder => builder.AddProvider(loggerProvider));
        });

        using var client = factory.CreateClient();
        const string password = "SuperSecretPassword123!";

        await client.PostAsJsonAsync(
            "/auth/signup",
            new { email = "user@example.com", password });

        Assert.DoesNotContain(password, string.Join('\n', loggerProvider.Messages), StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> CreateFactory(Action<IServiceCollection>? configure = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            AuthControllerValidationTests.ConfigureRequiredSettings(builder);
            builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:4200");
            if (configure is not null)
            {
                builder.ConfigureServices(configure);
            }
        });

    private sealed class StubAuthFunctionClient : IAuthFunctionClient
    {
        private readonly FunctionCallResult<SignupResponse> _signup;
        private readonly FunctionCallResult<LoginResponse> _login;

        public StubAuthFunctionClient(
            FunctionCallResult<SignupResponse>? signup = null,
            FunctionCallResult<LoginResponse>? login = null)
        {
            _signup = signup ?? FunctionCallResult<SignupResponse>.Ok(new SignupResponse("ok", "user@example.com"));
            _login = login ?? FunctionCallResult<LoginResponse>.Ok(new LoginResponse("ok", "user@example.com"));
        }

        public Task<FunctionCallResult<SignupResponse>> SignupAsync(SignupRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(_signup);

        public Task<FunctionCallResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(_login);
    }
}
