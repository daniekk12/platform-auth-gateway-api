using Microsoft.AspNetCore.Mvc.Filters;

namespace Platform.Auth.Gateway.Api.Filters;

public sealed class AuthResponseCacheFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        var headers = context.HttpContext.Response.Headers;
        headers.CacheControl = "no-store, no-cache, must-revalidate";
        headers.Pragma = "no-cache";
        headers.Expires = "0";
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
