using Microsoft.AspNetCore.Mvc;
using Platform.Auth.Gateway.Api.Contracts;
using Platform.Auth.Gateway.Api.Services;

namespace Platform.Auth.Gateway.Api.Tests;

public sealed class FunctionCallResultMapperTests
{
    [Fact]
    public void ToActionResult_propagates_downstream_4xx_status()
    {
        var result = FunctionCallResult<SignupResponse>.DownstreamError(
            StatusCodes.Status400BadRequest,
            "{\"title\":\"invalid\"}");

        var actionResult = FunctionCallResultMapper.ToActionResult(result);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_genericizes_downstream_4xx_when_body_contains_url()
    {
        var result = FunctionCallResult<SignupResponse>.DownstreamError(
            StatusCodes.Status400BadRequest,
            """{"title":"bad","detail":"see https://internal.function/error"}""");

        var actionResult = FunctionCallResultMapper.ToActionResult(result);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var body = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
        Assert.Contains("Request rejected by downstream function", body, StringComparison.Ordinal);
        Assert.DoesNotContain("https://", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToActionResult_maps_infrastructure_unavailable_to_503()
    {
        var result = FunctionCallResult<LoginResponse>.InfrastructureFailure(
            FunctionCallFailureKind.Unavailable);

        var actionResult = FunctionCallResultMapper.ToActionResult(result);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }
}
