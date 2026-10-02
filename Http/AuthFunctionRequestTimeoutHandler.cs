using Microsoft.Extensions.Options;
using Platform.Auth.Gateway.Api.Configuration;

namespace Platform.Auth.Gateway.Api.Http;

/// <summary>
/// Enforces per-request timeouts for auth function calls without retries.
/// </summary>
public sealed class AuthFunctionRequestTimeoutHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<FunctionEndpointsOptions> _options;

    public AuthFunctionRequestTimeoutHandler(IOptionsMonitor<FunctionEndpointsOptions> options)
    {
        _options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_options.CurrentValue.TimeoutSeconds);
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        return await base.SendAsync(request, linkedCts.Token);
    }
}
