using Microsoft.AspNetCore.Http;

namespace Platform.Auth.Gateway.Api.Http;

public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationHeaderName = "X-Correlation-ID";
    private const string TraceParentHeaderName = "traceparent";
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
        if (httpContext is not null)
        {
            if (!request.Headers.Contains(CorrelationHeaderName))
            {
                request.Headers.TryAddWithoutValidation(CorrelationHeaderName, httpContext.TraceIdentifier);
            }

            if (httpContext.Request.Headers.TryGetValue(TraceParentHeaderName, out var traceParent)
                && !request.Headers.Contains(TraceParentHeaderName))
            {
                request.Headers.TryAddWithoutValidation(TraceParentHeaderName, traceParent.ToString());
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
