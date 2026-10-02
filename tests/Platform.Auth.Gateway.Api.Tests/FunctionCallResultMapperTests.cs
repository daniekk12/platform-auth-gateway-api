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
    public void ToActionResult_maps_infrastructure_unavailable_to_503()
    {
        var result = FunctionCallResult<LoginResponse>.InfrastructureFailure(
            FunctionCallFailureKind.Unavailable);

        var actionResult = FunctionCallResultMapper.ToActionResult(result);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }
}
