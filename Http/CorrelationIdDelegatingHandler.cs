using Microsoft.AspNetCore.Http;

namespace Platform.Auth.Gateway.Api.Http;

public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationHeaderName = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null
            && !request.Headers.Contains(CorrelationHeaderName))
        {
            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, httpContext.TraceIdentifier);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
