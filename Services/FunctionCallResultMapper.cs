using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Auth.Gateway.Api.Services;

public static class FunctionCallResultMapper
{
    public static IActionResult ToActionResult<T>(FunctionCallResult<T> result)
        where T : class
    {
        if (result.IsSuccess && result.Payload is not null)
        {
            return new ObjectResult(result.Payload) { StatusCode = result.StatusCode };
        }

        if (result.FailureKind is not null)
        {
            return new ObjectResult(CreateProblem(result.StatusCode, GetInfrastructureTitle(result.FailureKind.Value)))
            {
                StatusCode = result.StatusCode
            };
        }

        if (result.StatusCode is >= 400 and < 500)
        {
            return CreateDownstreamClientErrorResult(result);
        }

        return new ObjectResult(CreateProblem(result.StatusCode, "Downstream function error"))
        {
            StatusCode = result.StatusCode >= 500 ? result.StatusCode : StatusCodes.Status502BadGateway
        };
    }

    private static IActionResult CreateDownstreamClientErrorResult<T>(FunctionCallResult<T> result)
        where T : class
    {
        if (TryParseJsonObject(result.ResponseBody, out var document))
        {
            return new ObjectResult(document) { StatusCode = result.StatusCode };
        }

        return new ObjectResult(CreateProblem(result.StatusCode, "Request rejected by downstream function"))
        {
            StatusCode = result.StatusCode
        };
    }

    private static object CreateProblem(int statusCode, string title) =>
        new
        {
            title,
            status = statusCode
        };

    private static string GetInfrastructureTitle(FunctionCallFailureKind kind) =>
        kind switch
        {
            FunctionCallFailureKind.Timeout => "Function call timed out",
            FunctionCallFailureKind.Unavailable => "Function unavailable",
            FunctionCallFailureKind.NotConfigured => "Function endpoint not configured",
            FunctionCallFailureKind.InvalidResponse => "Invalid function response",
            _ => "Gateway error"
        };

    private static bool TryParseJsonObject(string? body, out JsonElement document)
    {
        document = default;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.ValueKind is JsonValueKind.Object)
            {
                document = json.RootElement.Clone();
                return true;
            }
        }
        catch (JsonException)
        {
            // ignored
        }

        return false;
    }
}
