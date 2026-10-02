namespace Platform.Auth.Gateway.Api.Services;

public sealed class FunctionCallResult<T>
    where T : class
{
    public T? Payload { get; init; }

    public int StatusCode { get; init; }

    public string? ResponseBody { get; init; }

    public FunctionCallFailureKind? FailureKind { get; init; }

    public bool IsSuccess => FailureKind is null && StatusCode is >= 200 and < 300;

    public static FunctionCallResult<T> Ok(T payload, int statusCode = StatusCodes.Status200OK) =>
        new() { Payload = payload, StatusCode = statusCode };

    public static FunctionCallResult<T> DownstreamError(int statusCode, string? responseBody) =>
        new() { StatusCode = statusCode, ResponseBody = responseBody };

    public static FunctionCallResult<T> InfrastructureFailure(FunctionCallFailureKind kind) =>
        new() { FailureKind = kind, StatusCode = MapFailureToStatusCode(kind) };

    private static int MapFailureToStatusCode(FunctionCallFailureKind kind) =>
        kind switch
        {
            FunctionCallFailureKind.Timeout => StatusCodes.Status504GatewayTimeout,
            FunctionCallFailureKind.Unavailable => StatusCodes.Status503ServiceUnavailable,
            FunctionCallFailureKind.InvalidResponse => StatusCodes.Status502BadGateway,
            FunctionCallFailureKind.NotConfigured => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status502BadGateway
        };
}

public enum FunctionCallFailureKind
{
    NotConfigured,
    Unavailable,
    Timeout,
    InvalidResponse
}
