using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Platform.Auth.Gateway.Api.Configuration;
using Platform.Auth.Gateway.Api.Contracts;

namespace Platform.Auth.Gateway.Api.Services;

public sealed class AuthFunctionClient : IAuthFunctionClient
{
    public const string HttpClientName = "AuthFunctions";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FunctionEndpointsOptions _options;
    private readonly ILogger<AuthFunctionClient> _logger;

    public AuthFunctionClient(
        IHttpClientFactory httpClientFactory,
        IOptions<FunctionEndpointsOptions> options,
        ILogger<AuthFunctionClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public Task<FunctionCallResult<SignupResponse>> SignupAsync(
        SignupRequest request,
        CancellationToken cancellationToken) =>
        InvokeAsync<SignupRequest, SignupResponse>(
            operationName: "signup",
            baseUrl: _options.SignupUrl,
            relativePath: "signup",
            request,
            cancellationToken);

    public Task<FunctionCallResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        InvokeAsync<LoginRequest, LoginResponse>(
            operationName: "login",
            baseUrl: _options.LoginUrl,
            relativePath: "login",
            request,
            cancellationToken);

    private async Task<FunctionCallResult<TResponse>> InvokeAsync<TRequest, TResponse>(
        string operationName,
        string baseUrl,
        string relativePath,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogError("{Operation} function endpoint is not configured", operationName);
            return FunctionCallResult<TResponse>.InfrastructureFailure(FunctionCallFailureKind.NotConfigured);
        }

        var requestUri = BuildRequestUri(baseUrl, relativePath);

        _logger.LogInformation("Calling {Operation} function", operationName);

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var response = await client.PostAsJsonAsync(requestUri, request, cancellationToken);

            _logger.LogInformation(
                "{Operation} function returned {StatusCode}",
                operationName,
                (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return FunctionCallResult<TResponse>.DownstreamError((int)response.StatusCode, responseBody);
            }

            TResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
            }
            catch (Exception ex) when (ex is NotSupportedException or System.Text.Json.JsonException)
            {
                _logger.LogWarning(ex, "{Operation} function returned an invalid response body", operationName);
                return FunctionCallResult<TResponse>.InfrastructureFailure(FunctionCallFailureKind.InvalidResponse);
            }

            if (payload is null)
            {
                _logger.LogWarning("{Operation} function returned an empty response body", operationName);
                return FunctionCallResult<TResponse>.InfrastructureFailure(FunctionCallFailureKind.InvalidResponse);
            }

            return FunctionCallResult<TResponse>.Ok(payload, (int)response.StatusCode);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "{Operation} function call timed out", operationName);
            return FunctionCallResult<TResponse>.InfrastructureFailure(FunctionCallFailureKind.Timeout);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "{Operation} function is unavailable", operationName);
            return FunctionCallResult<TResponse>.InfrastructureFailure(FunctionCallFailureKind.Unavailable);
        }
    }

    private static Uri BuildRequestUri(string baseUrl, string relativePath)
    {
        var trimmedBase = baseUrl.TrimEnd('/');
        return new Uri($"{trimmedBase}/{relativePath.TrimStart('/')}");
    }
}
