using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.Auth.Gateway.Api.Configuration;
using Platform.Auth.Gateway.Api.Contracts;
using Platform.Auth.Gateway.Api.Services;

namespace Platform.Auth.Gateway.Api.Tests;

public sealed class AuthFunctionClientTests
{
    private const string SignupBaseUrl = "http://signup-function.test";
    private const string LoginBaseUrl = "http://login-function.test";

    [Fact]
    public async Task SignupAsync_posts_to_signup_path_on_configured_base_url()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(JsonOk(new SignupResponse("Signup function executed", "user@example.com"))));

        var client = CreateClient(handler, timeoutSeconds: 30);

        var result = await client.SignupAsync(
            new SignupRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal($"{SignupBaseUrl}/signup", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task LoginAsync_posts_to_login_path_on_configured_base_url()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(JsonOk(new LoginResponse("Login function executed", "user@example.com"))));

        var client = CreateClient(handler, timeoutSeconds: 30);

        var result = await client.LoginAsync(
            new LoginRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal($"{LoginBaseUrl}/login", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SignupAsync_propagates_downstream_4xx()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"title\":\"invalid\"}", Encoding.UTF8, "application/json")
            }));

        var client = CreateClient(handler, timeoutSeconds: 30);

        var result = await client.SignupAsync(
            new SignupRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Null(result.FailureKind);
    }

    [Fact]
    public async Task LoginAsync_treats_downstream_5xx_as_downstream_error()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"title\":\"failure\"}", Encoding.UTF8, "application/json")
            }));

        var client = CreateClient(handler, timeoutSeconds: 30);

        var result = await client.LoginAsync(
            new LoginRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Null(result.FailureKind);
    }

    [Fact]
    public async Task SignupAsync_returns_unavailable_when_function_cannot_be_reached()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new HttpRequestException("connection refused"));

        var client = CreateClient(handler, timeoutSeconds: 30);

        var result = await client.SignupAsync(
            new SignupRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        Assert.Equal(FunctionCallFailureKind.Unavailable, result.FailureKind);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_returns_timeout_when_function_does_not_respond_in_time()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return JsonOk(new LoginResponse("late", "user@example.com"));
        });

        var client = CreateClient(handler, timeoutSeconds: 1);

        var result = await client.LoginAsync(
            new LoginRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        Assert.Equal(FunctionCallFailureKind.Timeout, result.FailureKind);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, result.StatusCode);
    }

    [Fact]
    public async Task SignupAsync_uses_base_url_from_options_not_hardcoded_value()
    {
        const string customBase = "http://custom-signup-host:9001";
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(JsonOk(new SignupResponse("ok", "user@example.com"))));

        var client = CreateClient(
            handler,
            timeoutSeconds: 30,
            signupUrl: customBase,
            loginUrl: LoginBaseUrl);

        await client.SignupAsync(
            new SignupRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        Assert.Equal($"{customBase}/signup", handler.LastRequest!.RequestUri!.ToString());
    }

    private static AuthFunctionClient CreateClient(
        StubHttpMessageHandler handler,
        int timeoutSeconds,
        string signupUrl = SignupBaseUrl,
        string loginUrl = LoginBaseUrl)
    {
        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        var factory = new SingleHttpClientFactory(httpClient);

        var options = Options.Create(new FunctionEndpointsOptions
        {
            SignupUrl = signupUrl,
            LoginUrl = loginUrl,
            TimeoutSeconds = timeoutSeconds
        });

        return new AuthFunctionClient(factory, options, NullLogger<AuthFunctionClient>.Instance);
    }

    private static HttpResponseMessage JsonOk<T>(T payload) =>
        new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload)
        };

    private sealed class SingleHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => httpClient;
    }
}
